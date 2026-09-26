using System;
using System.Collections.Generic;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class AnalyticsComponent : IAnalyticsComponent
{
    private readonly IDatabaseComponent _db;

    public AnalyticsComponent(IDatabaseComponent db)
    {
        _db = db;
    }

    public StatsOverviewResponse GetOverview()
    {
        // 1. Streak calculation (consecutive days with completed tasks)
        var doneDates = _db.Query(@"
            SELECT DISTINCT DATE(done_at) as done_day 
            FROM tasks 
            WHERE status = 'done' AND done_at IS NOT NULL 
            ORDER BY done_day DESC
        ");

        int streak = 0;
        var today = DateTime.UtcNow.Date;
        var checkDate = today;

        var completedDateSet = new HashSet<string>();
        foreach (var r in doneDates)
        {
            if (r["done_day"] != null)
                completedDateSet.Add(r["done_day"]!.ToString()!);
        }

        // If today has no completed tasks yet, streak may still be alive from yesterday
        if (!completedDateSet.Contains(checkDate.ToString("yyyy-MM-dd")))
        {
            checkDate = checkDate.AddDays(-1);
        }

        while (completedDateSet.Contains(checkDate.ToString("yyyy-MM-dd")))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        // 2. Today's task counts
        var todayRow = _db.QuerySingle(@"
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN status = 'done' THEN 1 ELSE 0 END) as done
            FROM tasks 
            WHERE DATE(due_date) = DATE('now') OR DATE(created_at) = DATE('now')
        ");

        int todayTotal = todayRow != null && todayRow["total"] != null ? Convert.ToInt32(todayRow["total"]) : 0;
        int todayDone = todayRow != null && todayRow["done"] != null ? Convert.ToInt32(todayRow["done"]) : 0;

        // 3. Exam countdown
        var examDate = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        int daysLeft = Math.Max(0, (examDate - DateTime.UtcNow.Date).Days);

        // 4. Overall attempt accuracy
        var accRow = _db.QuerySingle(@"
            SELECT 
                COUNT(*) as total_attempts,
                SUM(CASE WHEN correct = 1 THEN 1 ELSE 0 END) as correct_attempts
            FROM attempts
        ");

        double accuracy = 0.0;
        if (accRow != null && accRow["total_attempts"] != null)
        {
            int totalAtt = Convert.ToInt32(accRow["total_attempts"]);
            int correctAtt = accRow["correct_attempts"] != null ? Convert.ToInt32(accRow["correct_attempts"]) : 0;
            if (totalAtt > 0)
            {
                accuracy = Math.Round((double)correctAtt / totalAtt * 100.0, 1);
            }
        }

        return new StatsOverviewResponse(streak, daysLeft, todayTotal, todayDone, accuracy);
    }

    public HeatmapResponse GetHeatmap(int days = 90)
    {
        var rows = _db.Query(@"
            SELECT DATE(done_at) as date_val, COUNT(*) as count_val
            FROM tasks 
            WHERE status = 'done' AND done_at IS NOT NULL 
              AND done_at >= datetime('now', '-' || @days || ' days')
            GROUP BY DATE(done_at)
            ORDER BY date_val ASC
        ", ("@days", days));

        var items = new List<HeatmapItem>();
        foreach (var r in rows)
        {
            string d = r["date_val"]?.ToString() ?? "";
            int c = r["count_val"] != null ? Convert.ToInt32(r["count_val"]) : 0;
            if (!string.IsNullOrEmpty(d))
            {
                items.Add(new HeatmapItem(d, c));
            }
        }
        return new HeatmapResponse(items);
    }

    public WeakTopicsResponse GetWeakTopics(int limit = 5)
    {
        var rows = _db.Query(@"
            SELECT 
                q.topic as topic_name,
                COUNT(a.id) as total_attempts,
                SUM(CASE WHEN a.correct = 0 THEN 1 ELSE 0 END) as mistakes,
                SUM(CASE WHEN a.correct = 1 THEN 1 ELSE 0 END) as correct_count
            FROM attempts a
            JOIN questions q ON a.question_id = q.id
            GROUP BY q.topic
            ORDER BY mistakes DESC, total_attempts DESC
            LIMIT @limit
        ", ("@limit", limit));

        var list = new List<WeakTopicItem>();
        foreach (var r in rows)
        {
            string topic = r["topic_name"]?.ToString() ?? "General";
            int total = r["total_attempts"] != null ? Convert.ToInt32(r["total_attempts"]) : 0;
            int mistakes = r["mistakes"] != null ? Convert.ToInt32(r["mistakes"]) : 0;
            int correct = r["correct_count"] != null ? Convert.ToInt32(r["correct_count"]) : 0;
            double acc = total > 0 ? Math.Round((double)correct / total * 100.0, 1) : 0.0;

            list.Add(new WeakTopicItem(topic, total, mistakes, acc));
        }

        return new WeakTopicsResponse(list);
    }

    public List<ForgettingRiskQuestion> GetForgettingRiskQuestions(int limit = 5)
    {
        // Query questions that have been reviewed or are due
        var rows = _db.Query(@"
            SELECT q.*, 
                   MAX(a.created_at) as last_attempt_date,
                   COUNT(a.id) as attempt_count,
                   SUM(CASE WHEN a.correct = 1 THEN 1 ELSE 0 END) as correct_count
            FROM questions q
            LEFT JOIN attempts a ON q.id = a.question_id
            GROUP BY q.id
            ORDER BY q.sr_due ASC, q.sr_reps ASC
            LIMIT @limit
        ", ("@limit", limit));

        var result = new List<ForgettingRiskQuestion>();
        var now = DateTime.UtcNow;

        foreach (var r in rows)
        {
            var q = new Question
            {
                Id = Convert.ToInt32(r["id"]),
                SubjectId = r["subject_id"] != null ? Convert.ToInt32(r["subject_id"]) : null,
                Topic = r["topic"]?.ToString() ?? "",
                Difficulty = r["difficulty"]?.ToString() ?? "medium",
                Stem = r["stem"]?.ToString() ?? "",
                OptionsJson = r["options_json"]?.ToString() ?? "[]",
                Answer = r["answer"]?.ToString() ?? "",
                SolutionMd = r["solution_md"]?.ToString() ?? "",
                Source = r["source"]?.ToString() ?? "GATE",
                TagsJson = r["tags_json"]?.ToString() ?? "[]",
                SrEase = r["sr_ease"] != null ? Convert.ToDouble(r["sr_ease"]) : 2.5,
                SrInterval = r["sr_interval"] != null ? Convert.ToInt32(r["sr_interval"]) : 0,
                SrDue = r["sr_due"]?.ToString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                SrReps = r["sr_reps"] != null ? Convert.ToInt32(r["sr_reps"]) : 0,
                CreatedAt = DateTime.TryParse(r["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
            };

            DateTime lastDate = DateTime.TryParse(r["last_attempt_date"]?.ToString(), out var ld) ? ld : q.CreatedAt;
            int daysSince = Math.Max(0, (now.Date - lastDate.Date).Days);
            
            // Ebbinghaus Forgetting Curve: R = e^(-t / S)
            double stability = Math.Max(1.0, q.SrInterval * (q.SrEase / 2.0));
            double retention = Math.Exp(-daysSince / stability);
            retention = Math.Round(Math.Clamp(retention * 100.0, 5.0, 99.0), 1);

            string risk = retention < 45.0 ? "Critical" : retention < 70.0 ? "High Risk" : "Moderate";

            result.Add(new ForgettingRiskQuestion(q, retention, daysSince, risk));
        }

        return result;
    }

    public AdvancedAnalyticsResponse GetAdvancedAnalytics()
    {
        var overview = GetOverview();
        var highRisk = GetForgettingRiskQuestions(5);

        // Calculate subject mastery
        var subRows = _db.Query(@"
            SELECT s.name as subject_name,
                   COUNT(q.id) as total_q,
                   SUM(CASE WHEN q.sr_reps >= 2 THEN 1 ELSE 0 END) as mastered_q
            FROM subjects s
            LEFT JOIN questions q ON s.id = q.subject_id
            WHERE s.parent_id IS NOT NULL OR s.name IN ('Algorithms', 'Operating Systems', 'Database Systems', 'Computer Networks', 'Theory of Computation')
            GROUP BY s.name
        ");

        var masteryList = new List<SubjectMastery>();
        foreach (var sr in subRows)
        {
            string sName = sr["subject_name"]?.ToString() ?? "CS";
            int tQ = sr["total_q"] != null ? Convert.ToInt32(sr["total_q"]) : 0;
            int mQ = sr["mastered_q"] != null ? Convert.ToInt32(sr["mastered_q"]) : 0;
            double pct = tQ > 0 ? Math.Round((double)mQ / tQ * 100.0, 1) : 0.0;

            masteryList.Add(new SubjectMastery(sName, tQ, mQ, pct));
        }

        if (masteryList.Count == 0)
        {
            masteryList.Add(new SubjectMastery("Data Structures & Algo", 15, 0, 0.0));
            masteryList.Add(new SubjectMastery("Operating Systems", 12, 0, 0.0));
            masteryList.Add(new SubjectMastery("Database Systems", 10, 0, 0.0));
            masteryList.Add(new SubjectMastery("Computer Networks", 10, 0, 0.0));
            masteryList.Add(new SubjectMastery("Theory of Computation", 8, 0, 0.0));
        }

        // Time spent distribution from real activity records
        var practiceSecRow = _db.QuerySingle("SELECT COALESCE(SUM(time_sec), 0) as s FROM attempts");
        int practiceMin = practiceSecRow != null && practiceSecRow["s"] != null ? Convert.ToInt32(practiceSecRow["s"]) / 60 : 0;

        var lectureSecRow = _db.QuerySingle("SELECT COALESCE(SUM(current_time_sec), 0) as s FROM lecture_trackers");
        int lectureMin = lectureSecRow != null && lectureSecRow["s"] != null ? Convert.ToInt32(lectureSecRow["s"]) / 60 : 0;

        var notesMinRow = _db.QuerySingle("SELECT COALESCE(SUM(actual_min), 0) as m FROM tasks WHERE title LIKE '%Note%' OR title LIKE '%Revision%'");
        int notesMin = notesMinRow != null && notesMinRow["m"] != null ? Convert.ToInt32(notesMinRow["m"]) : 0;

        var mockSecRow = _db.QuerySingle("SELECT COALESCE(SUM(time_taken_sec), 0) as s FROM mock_test_sessions");
        int mockMin = mockSecRow != null && mockSecRow["s"] != null ? Convert.ToInt32(mockSecRow["s"]) / 60 : 0;

        var timeSpent = new List<TimeSpentMetric>
        {
            new TimeSpentMetric("Practice & Quizzes", practiceMin),
            new TimeSpentMetric("Video Lectures", lectureMin),
            new TimeSpentMetric("Quick Revision Notes", notesMin),
            new TimeSpentMetric("Mock Problem Solving", mockMin)
        };

        // If no attempts or reviews exist, retention is initialized to 0.0%
        var attemptCountRow = _db.QuerySingle("SELECT COUNT(*) as c FROM attempts");
        int totalAttempts = attemptCountRow != null && attemptCountRow["c"] != null ? Convert.ToInt32(attemptCountRow["c"]) : 0;
        double avgRetention = (totalAttempts > 0 && highRisk.Count > 0)
            ? Math.Round(highRisk.Average(h => h.RetentionProbability), 1)
            : 0.0;

        return new AdvancedAnalyticsResponse(
            overview.Accuracy,
            avgRetention,
            masteryList,
            timeSpent,
            highRisk
        );
    }

    public void ResetAllMetrics()
    {
        _db.ExecuteNonQuery(@"
            DELETE FROM attempts;
            DELETE FROM mock_test_sessions;
            DELETE FROM sessions;
            UPDATE tasks SET status = 'pending', done_at = NULL, actual_min = 0;
            UPDATE questions SET sr_reps = 0, sr_interval = 0, sr_ease = 2.5, sr_due = date('now');
            UPDATE flashcards SET reps = 0, box = 1, interval_days = 1, last_reviewed_at = NULL, next_review_date = date('now');
            UPDATE lecture_trackers SET current_time_sec = 0, completed = 0, last_watched_at = NULL;
        ");
    }
}
