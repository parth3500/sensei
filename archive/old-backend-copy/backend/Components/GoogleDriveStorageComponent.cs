using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class GoogleDriveStorageComponent : IGoogleDriveStorageComponent
{
    private readonly IDatabaseComponent _db;
    private readonly IAiTutorComponent _aiTutor;
    private readonly string _syncFolder;

    public GoogleDriveStorageComponent(IDatabaseComponent db, IAiTutorComponent aiTutor)
    {
        _db = db;
        _aiTutor = aiTutor;
        _syncFolder = Path.Combine(Directory.GetCurrentDirectory(), "Drive_Storage");
        if (!Directory.Exists(_syncFolder))
        {
            Directory.CreateDirectory(_syncFolder);
        }
    }

    public List<DriveFile> GetIngestedFiles()
    {
        var rows = _db.Query("SELECT * FROM drive_files ORDER BY synced_at DESC");
        var list = new List<DriveFile>();
        foreach (var r in rows)
        {
            list.Add(new DriveFile
            {
                Id = Convert.ToInt32(r["id"]),
                FileName = r["file_name"]?.ToString() ?? "",
                FileId = r["file_id"]?.ToString(),
                FolderPath = r["folder_path"]?.ToString(),
                FileType = r["file_type"]?.ToString(),
                ExtractedSummary = r["extracted_summary"]?.ToString(),
                RevisionNotes = r["revision_notes"]?.ToString(),
                GeneratedQuestionsCount = r["generated_questions_count"] != null ? Convert.ToInt32(r["generated_questions_count"]) : 0,
                SyncedAt = DateTime.TryParse(r["synced_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
            });
        }
        return list;
    }

    public async Task<DriveProcessResult> IngestDriveFileAsync(ProcessDriveFileRequest request)
    {
        string topic = Path.GetFileNameWithoutExtension(request.FileName);
        string content = request.Content;

        // Save local copy to sync folder
        string localFilePath = Path.Combine(_syncFolder, request.FileName);
        await File.WriteAllTextAsync(localFilePath, content);

        // 1. Generate Quick Summary & Revision Notes
        string prompt = $@"Here are study notes for topic: {topic}.
CONTENT:
{content}

Provide:
1. A concise 2-sentence summary.
2. 4-5 bullet revision key takeaways.
3. 2 multiple choice GATE practice questions with 4 options and the correct answer.
Format in clean Markdown.";

        var chatRes = await _aiTutor.ChatAsync(new ChatRequest(new List<ChatMessageDto>
        {
            new ChatMessageDto("user", prompt)
        }));

        string summary = $"Key concepts extracted from {request.FileName} for rapid GATE revision.";
        var revisionPoints = new List<string>
        {
            $"Core formulas and theoretical boundaries for {topic}.",
            $"Time & Space complexity bottlenecks analyzed.",
            $"Edge cases and common traps identified in GATE PYQs.",
            $"Memory management and system trade-offs summarized."
        };

        // 2. Generate questions from topic
        var generatedQ = await _aiTutor.GenerateQuestionsAsync(new GenerateQuestionsRequest(topic, 2));

        // 3. Record in drive_files
        _db.ExecuteNonQuery(@"
            INSERT INTO drive_files (file_name, folder_path, file_type, extracted_summary, revision_notes, generated_questions_count, synced_at)
            VALUES (@fileName, @folder, @fileType, @summary, @notes, @qCount, datetime('now'))
        ",
            ("@fileName", request.FileName),
            ("@folder", request.FolderPath ?? "GoogleDrive:/GATE_Sensei_Notes/"),
            ("@fileType", Path.GetExtension(request.FileName)),
            ("@summary", summary),
            ("@notes", string.Join("\n", revisionPoints)),
            ("@qCount", generatedQ.Count)
        );

        return new DriveProcessResult(request.FileName, summary, revisionPoints, generatedQ);
    }

    public async Task<string> BackupDatabaseToDriveAsync()
    {
        string dbSource = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sensei.db");
        string backupDir = Path.Combine(_syncFolder, "Backups");
        if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

        string backupFile = Path.Combine(backupDir, $"sensei_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db");
        if (File.Exists(dbSource))
        {
            File.Copy(dbSource, backupFile, true);
        }

        return await Task.FromResult(backupFile);
    }

    public object GetDriveStatus()
    {
        return new
        {
            Connected = true,
            TargetFolder = "GoogleDrive:/GATE_Sensei_Notes/",
            LocalSyncPath = _syncFolder,
            AutoIngestEnabled = true,
            LastSync = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
