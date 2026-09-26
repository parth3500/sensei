using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class VideoLectureComponent : IVideoLectureComponent
{
    private readonly IDatabaseComponent _db;

    public VideoLectureComponent(IDatabaseComponent db)
    {
        _db = db;
    }

    public static string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Check Google Drive file
        var driveMatch = Regex.Match(url, @"(?:drive\.google\.com\/(?:file\/d\/|open\?id=))([a-zA-Z0-9_-]+)");
        if (driveMatch.Success)
        {
            return $"drive:{driveMatch.Groups[1].Value}";
        }

        // Check YouTube
        var m1 = Regex.Match(url, @"(?:youtu\.be\/|v\/|u\/\w\/|embed\/|watch\?v=)([^#\&\?]{11})");
        if (m1.Success) return m1.Groups[1].Value;

        var m2 = Regex.Match(url, @"^[a-zA-Z0-9_-]{11}$");
        if (m2.Success) return url;

        return null;
    }

    private LectureTracker MapLecture(Dictionary<string, object?> row)
    {
        return new LectureTracker
        {
            Id = Convert.ToInt32(row["id"]),
            Title = row["title"]?.ToString() ?? "",
            VideoUrl = row["video_url"]?.ToString() ?? "",
            VideoId = row["video_id"]?.ToString(),
            CurrentTimeSec = row["current_time_sec"] != null ? Convert.ToInt32(row["current_time_sec"]) : 0,
            TotalDurationSec = row["total_duration_sec"] != null ? Convert.ToInt32(row["total_duration_sec"]) : 0,
            Completed = Convert.ToInt32(row["completed"]) == 1,
            Notes = row["notes"]?.ToString(),
            SubjectId = row["subject_id"] != null ? Convert.ToInt32(row["subject_id"]) : null,
            LastWatchedAt = DateTime.TryParse(row["last_watched_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow,
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var ct) ? ct : DateTime.UtcNow
        };
    }

    public List<LectureTracker> GetLectures()
    {
        var rows = _db.Query("SELECT * FROM lecture_trackers ORDER BY last_watched_at DESC, id DESC");
        var list = new List<LectureTracker>();
        foreach (var r in rows)
        {
            list.Add(MapLecture(r));
        }
        return list;
    }

    public LectureTracker? GetLectureById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM lecture_trackers WHERE id = @id", ("@id", id));
        return row != null ? MapLecture(row) : null;
    }

    public LectureTracker CreateLecture(CreateLectureRequest request)
    {
        string? videoId = ExtractVideoId(request.VideoUrl);

        long newId = _db.InsertAndGetId(@"
            INSERT INTO lecture_trackers (title, video_url, video_id, current_time_sec, total_duration_sec, completed, subject_id, last_watched_at, created_at)
            VALUES (@title, @videoUrl, @videoId, 0, @duration, 0, @subjectId, datetime('now'), datetime('now'))
        ",
            ("@title", request.Title),
            ("@videoUrl", request.VideoUrl),
            ("@videoId", videoId),
            ("@duration", request.TotalDurationSec ?? 3600),
            ("@subjectId", request.SubjectId)
        );

        return GetLectureById((int)newId)!;
    }

    public LectureTracker? UpdateProgress(int id, UpdateLectureProgressRequest request)
    {
        var sets = new List<string>
        {
            "current_time_sec = @timeSec",
            "last_watched_at = datetime('now')"
        };
        var parameters = new List<(string Name, object? Value)>
        {
            ("@id", id),
            ("@timeSec", request.CurrentTimeSec)
        };

        if (request.TotalDurationSec.HasValue)
        {
            sets.Add("total_duration_sec = @duration");
            parameters.Add(("@duration", request.TotalDurationSec.Value));
        }

        if (request.Completed.HasValue)
        {
            sets.Add("completed = @completed");
            parameters.Add(("@completed", request.Completed.Value ? 1 : 0));
        }
        else if (request.TotalDurationSec.HasValue && request.TotalDurationSec > 0 && request.CurrentTimeSec >= (request.TotalDurationSec.Value * 0.95))
        {
            sets.Add("completed = 1");
        }

        if (request.Notes != null)
        {
            sets.Add("notes = @notes");
            parameters.Add(("@notes", request.Notes));
        }

        string sql = $"UPDATE lecture_trackers SET {string.Join(", ", sets)} WHERE id = @id";
        _db.ExecuteNonQuery(sql, parameters.ToArray());

        return GetLectureById(id);
    }

    public bool DeleteLecture(int id)
    {
        int affected = _db.ExecuteNonQuery("DELETE FROM lecture_trackers WHERE id = @id", ("@id", id));
        return affected > 0;
    }

    public YouTubeSyncResult SyncYouTubeHistory(YouTubeSyncRequest request)
    {
        int newCount = 0;
        int updatedCount = 0;

        var itemsToProcess = new List<YouTubeSyncItem>();

        if (request.HistoryItems != null && request.HistoryItems.Count > 0)
        {
            itemsToProcess.AddRange(request.HistoryItems);
        }

        // Process explicit URLs
        if (request.Urls != null)
        {
            foreach (var url in request.Urls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                string? vId = ExtractVideoId(url);
                itemsToProcess.Add(new YouTubeSyncItem
                {
                    Url = url.Trim(),
                    VideoId = vId,
                    Title = $"Lecture ({vId ?? "Video"})",
                    TotalDurationSec = 3600
                });
            }
        }

        // Parse raw text for youtube and drive links
        if (!string.IsNullOrWhiteSpace(request.RawText))
        {
            var urlMatches = Regex.Matches(request.RawText, @"https?://[^\s<>""']+");
            foreach (Match m in urlMatches)
            {
                string u = m.Value;
                string? vId = ExtractVideoId(u);
                if (vId != null)
                {
                    itemsToProcess.Add(new YouTubeSyncItem
                    {
                        Url = u,
                        VideoId = vId,
                        Title = $"Synced Lecture ({vId})",
                        TotalDurationSec = 3600
                    });
                }
            }
        }

        foreach (var item in itemsToProcess)
        {
            string url = item.Url ?? "";
            string? videoId = item.VideoId ?? ExtractVideoId(url);
            if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(videoId)) continue;

            if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(videoId))
            {
                url = videoId.StartsWith("drive:") 
                    ? $"https://drive.google.com/file/d/{videoId.Replace("drive:", "")}/view" 
                    : $"https://www.youtube.com/watch?v={videoId}";
            }

            // Find existing
            var existing = _db.QuerySingle(
                "SELECT * FROM lecture_trackers WHERE video_id = @vId OR video_url = @vUrl LIMIT 1",
                ("@vId", videoId),
                ("@vUrl", url)
            );

            int duration = item.TotalDurationSec ?? 3600;
            int currentTime = item.CurrentTimeSec ?? (item.Completed == true ? duration : 0);
            bool isCompleted = item.Completed ?? (currentTime >= (duration * 0.9));

            if (existing != null)
            {
                int exId = Convert.ToInt32(existing["id"]);
                int exTime = Convert.ToInt32(existing["current_time_sec"]);
                // Only update progress if new time is greater
                int targetTime = Math.Max(exTime, currentTime);
                bool targetCompleted = isCompleted || (targetTime >= (duration * 0.9));

                _db.ExecuteNonQuery(@"
                    UPDATE lecture_trackers 
                    SET current_time_sec = @cTime, completed = @comp, last_watched_at = datetime('now')
                    WHERE id = @id
                ",
                    ("@cTime", targetTime),
                    ("@comp", targetCompleted ? 1 : 0),
                    ("@id", exId)
                );
                updatedCount++;
            }
            else
            {
                string title = !string.IsNullOrWhiteSpace(item.Title) ? item.Title : $"Lecture ({videoId ?? "Video"})";
                _db.ExecuteNonQuery(@"
                    INSERT INTO lecture_trackers (title, video_url, video_id, current_time_sec, total_duration_sec, completed, last_watched_at, created_at)
                    VALUES (@title, @url, @vId, @cTime, @dur, @comp, datetime('now'), datetime('now'))
                ",
                    ("@title", title),
                    ("@url", url),
                    ("@vId", videoId),
                    ("@cTime", currentTime),
                    ("@dur", duration),
                    ("@comp", isCompleted ? 1 : 0)
                );
                newCount++;
            }
        }

        return new YouTubeSyncResult
        {
            SyncedCount = newCount + updatedCount,
            NewLecturesCount = newCount,
            UpdatedCount = updatedCount,
            Message = $"Successfully synced {newCount + updatedCount} lectures ({newCount} new, {updatedCount} updated progress)."
        };
    }
}
