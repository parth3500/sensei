using System;
using System.Collections.Generic;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class ModuleManagerComponent : IModuleManagerComponent
{
    private readonly IDatabaseComponent _db;

    public ModuleManagerComponent(IDatabaseComponent db)
    {
        _db = db;
        EnsureDefaultModules();
    }

    private void EnsureDefaultModules()
    {
        var defaults = new (string Key, string Title, string Author, string Desc, string Icon)[]
        {
            ("video_tracker", "Video Lecture Tracker", "Core", "Track timestamps and completion across GATE video courses", "Video"),
            ("drive_notes", "Google Drive Ingest", "Core", "Auto-generate notes, flashcards and questions from Drive documents", "FolderGit2"),
            ("ebbinghaus_decay", "Forgetting Curve Recall", "Core", "Prioritize questions and concepts fading from memory", "BrainCircuit"),
            ("formula_vault", "Formula & Theorem Vault", "Community", "Quick reference sheet for Math, Algo & OS formulas", "BookMarked"),
            ("weightage_analysis", "Weightage & Syllabus Analytics", "Core", "Historical marks distribution across GATE CS subjects, high-yield topic recommendations, and weighted test generator", "BarChart3"),
            ("flashcards", "Flashcards & Leitner Box", "Core", "Active recall spaced repetition system with 5 Leitner review boxes for GATE CS", "Layers")
        };


        foreach (var (key, title, author, desc, icon) in defaults)
        {
            _db.ExecuteNonQuery(@"
                INSERT OR IGNORE INTO study_modules (module_key, title, author, description, icon, enabled, created_at)
                VALUES (@key, @title, @author, @desc, @icon, 1, datetime('now'))
            ", ("@key", key), ("@title", title), ("@author", author), ("@desc", desc), ("@icon", icon));
        }
    }

    public List<StudyModule> GetModules()
    {
        var rows = _db.Query("SELECT * FROM study_modules ORDER BY id ASC");
        var list = new List<StudyModule>();
        foreach (var r in rows)
        {
            list.Add(new StudyModule
            {
                Id = Convert.ToInt32(r["id"]),
                ModuleKey = r["module_key"]?.ToString() ?? "",
                Title = r["title"]?.ToString() ?? "",
                Author = r["author"]?.ToString(),
                Description = r["description"]?.ToString(),
                Icon = r["icon"]?.ToString() ?? "Layers",
                Enabled = Convert.ToInt32(r["enabled"]) == 1,
                ConfigJson = r["config_json"]?.ToString() ?? "{}",
                CreatedAt = DateTime.TryParse(r["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
            });
        }
        return list;
    }

    public StudyModule RegisterModule(RegisterModuleRequest request)
    {
        long newId = _db.InsertAndGetId(@"
            INSERT OR REPLACE INTO study_modules (module_key, title, author, description, icon, enabled, created_at)
            VALUES (@key, @title, @author, @desc, @icon, 1, datetime('now'))
        ",
            ("@key", request.ModuleKey),
            ("@title", request.Title),
            ("@author", request.Author ?? "Collaborator"),
            ("@desc", request.Description ?? "Plugged module component"),
            ("@icon", request.Icon)
        );

        return GetModules().Find(m => m.ModuleKey == request.ModuleKey)!;
    }

    public bool ToggleModule(int id, bool enabled)
    {
        int affected = _db.ExecuteNonQuery("UPDATE study_modules SET enabled = @enabled WHERE id = @id",
            ("@enabled", enabled ? 1 : 0), ("@id", id));
        return affected > 0;
    }
}
