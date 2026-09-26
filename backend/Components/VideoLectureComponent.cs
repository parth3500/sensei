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

    private static string? ExtractYouTubeId(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

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
        string? videoId = ExtractYouTubeId(request.VideoUrl);

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
}
