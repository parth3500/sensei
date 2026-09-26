using System;
using System.Collections.Generic;
using System.Text;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class TaskComponent : ITaskComponent
{
    private readonly IDatabaseComponent _db;

    public TaskComponent(IDatabaseComponent db)
    {
        _db = db;
    }

    private TaskItem MapTask(Dictionary<string, object?> row)
    {
        return new TaskItem
        {
            Id = Convert.ToInt32(row["id"]),
            Title = row["title"]?.ToString() ?? "",
            SubjectId = row["subject_id"] != null ? Convert.ToInt32(row["subject_id"]) : null,
            Topic = row["topic"]?.ToString(),
            DueDate = row["due_date"]?.ToString(),
            Priority = row["priority"]?.ToString() ?? "medium",
            Status = row["status"]?.ToString() ?? "pending",
            EstMin = row["est_min"] != null ? Convert.ToInt32(row["est_min"]) : null,
            ActualMin = row["actual_min"] != null ? Convert.ToInt32(row["actual_min"]) : 0,
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow,
            DoneAt = DateTime.TryParse(row["done_at"]?.ToString(), out var done) ? done : null
        };
    }

    public List<TaskItem> GetTasks(string? status = null, string? date = null, int? subjectId = null)
    {
        var sb = new StringBuilder("SELECT * FROM tasks WHERE 1=1");
        var parameters = new List<(string Name, object? Value)>();

        if (!string.IsNullOrEmpty(status))
        {
            sb.Append(" AND status = @status");
            parameters.Add(("@status", status));
        }

        if (!string.IsNullOrEmpty(date))
        {
            sb.Append(" AND DATE(due_date) = DATE(@date)");
            parameters.Add(("@date", date));
        }

        if (subjectId.HasValue)
        {
            sb.Append(" AND subject_id = @subjectId");
            parameters.Add(("@subjectId", subjectId.Value));
        }

        sb.Append(" ORDER BY CASE WHEN status = 'pending' THEN 0 ELSE 1 END, due_date ASC, id DESC");

        var rows = _db.Query(sb.ToString(), parameters.ToArray());
        var list = new List<TaskItem>();
        foreach (var r in rows)
        {
            list.Add(MapTask(r));
        }
        return list;
    }

    public TaskItem? GetTaskById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM tasks WHERE id = @id", ("@id", id));
        return row != null ? MapTask(row) : null;
    }

    public TaskItem CreateTask(TaskCreateRequest request)
    {
        long newId = _db.InsertAndGetId(@"
            INSERT INTO tasks (title, subject_id, topic, due_date, priority, status, est_min, actual_min, created_at)
            VALUES (@title, @subjectId, @topic, @dueDate, @priority, 'pending', @estMin, 0, datetime('now'))
        ",
            ("@title", request.Title),
            ("@subjectId", request.SubjectId),
            ("@topic", request.Topic),
            ("@dueDate", request.DueDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd")),
            ("@priority", string.IsNullOrEmpty(request.Priority) ? "medium" : request.Priority),
            ("@estMin", request.EstMin)
        );

        return GetTaskById((int)newId)!;
    }

    public TaskItem? UpdateTask(int id, TaskUpdateRequest request)
    {
        var existing = GetTaskById(id);
        if (existing == null) return null;

        var sets = new List<string>();
        var parameters = new List<(string Name, object? Value)> { ("@id", id) };

        if (!string.IsNullOrEmpty(request.Title))
        {
            sets.Add("title = @title");
            parameters.Add(("@title", request.Title));
        }

        if (!string.IsNullOrEmpty(request.Priority))
        {
            sets.Add("priority = @priority");
            parameters.Add(("@priority", request.Priority));
        }

        if (!string.IsNullOrEmpty(request.DueDate))
        {
            sets.Add("due_date = @dueDate");
            parameters.Add(("@dueDate", request.DueDate));
        }

        if (request.ActualMin.HasValue)
        {
            sets.Add("actual_min = @actualMin");
            parameters.Add(("@actualMin", request.ActualMin.Value));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            sets.Add("status = @status");
            parameters.Add(("@status", request.Status));

            if (request.Status.Equals("done", StringComparison.OrdinalIgnoreCase))
            {
                sets.Add("done_at = datetime('now')");
            }
            else
            {
                sets.Add("done_at = NULL");
            }
        }

        if (sets.Count > 0)
        {
            string sql = $"UPDATE tasks SET {string.Join(", ", sets)} WHERE id = @id";
            _db.ExecuteNonQuery(sql, parameters.ToArray());
        }

        return GetTaskById(id);
    }

    public bool DeleteTask(int id)
    {
        int affected = _db.ExecuteNonQuery("DELETE FROM tasks WHERE id = @id", ("@id", id));
        return affected > 0;
    }
}
