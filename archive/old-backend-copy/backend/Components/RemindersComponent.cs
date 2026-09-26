using System;
using System.Collections.Generic;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class RemindersComponent : IRemindersComponent
{
    private readonly IDatabaseComponent _db;

    public RemindersComponent(IDatabaseComponent db)
    {
        _db = db;
    }

    public List<Reminder> GetReminders()
    {
        var rows = _db.Query("SELECT * FROM reminders ORDER BY id ASC");
        var list = new List<Reminder>();
        foreach (var r in rows)
        {
            list.Add(new Reminder
            {
                Id = Convert.ToInt32(r["id"]),
                Title = r["title"]?.ToString() ?? "",
                Cron = r["cron"]?.ToString() ?? "",
                Channel = r["channel"]?.ToString() ?? "webpush",
                PayloadJson = r["payload_json"]?.ToString() ?? "{}",
                Enabled = Convert.ToInt32(r["enabled"]) == 1,
                LastFired = DateTime.TryParse(r["last_fired"]?.ToString(), out var dt) ? dt : null
            });
        }
        return list;
    }

    public Reminder? UpdateReminder(int id, ReminderUpdateRequest request)
    {
        var sets = new List<string>();
        var parameters = new List<(string Name, object? Value)> { ("@id", id) };

        if (request.Enabled.HasValue)
        {
            sets.Add("enabled = @enabled");
            parameters.Add(("@enabled", request.Enabled.Value ? 1 : 0));
        }

        if (!string.IsNullOrEmpty(request.Cron))
        {
            sets.Add("cron = @cron");
            parameters.Add(("@cron", request.Cron));
        }

        if (!string.IsNullOrEmpty(request.Title))
        {
            sets.Add("title = @title");
            parameters.Add(("@title", request.Title));
        }

        if (sets.Count > 0)
        {
            string sql = $"UPDATE reminders SET {string.Join(", ", sets)} WHERE id = @id";
            _db.ExecuteNonQuery(sql, parameters.ToArray());
        }

        var row = _db.QuerySingle("SELECT * FROM reminders WHERE id = @id", ("@id", id));
        if (row == null) return null;

        return new Reminder
        {
            Id = Convert.ToInt32(row["id"]),
            Title = row["title"]?.ToString() ?? "",
            Cron = row["cron"]?.ToString() ?? "",
            Channel = row["channel"]?.ToString() ?? "webpush",
            PayloadJson = row["payload_json"]?.ToString() ?? "{}",
            Enabled = Convert.ToInt32(row["enabled"]) == 1
        };
    }

    public bool SavePushSubscription(PushSubscription subscription)
    {
        int count = _db.ExecuteNonQuery(@"
            INSERT OR REPLACE INTO push_subs (endpoint, p256dh, auth, ua, created_at)
            VALUES (@endpoint, @p256dh, @auth, @ua, datetime('now'))
        ",
            ("@endpoint", subscription.Endpoint),
            ("@p256dh", subscription.P256dh),
            ("@auth", subscription.Auth),
            ("@ua", subscription.Ua)
        );
        return count > 0;
    }
}
