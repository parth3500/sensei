using System;
using System.Collections.Generic;
using System.Text;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class PracticeComponent : IPracticeComponent
{
    private readonly IDatabaseComponent _db;
    private readonly ISpacedRepetitionComponent _sr;

    public PracticeComponent(IDatabaseComponent db, ISpacedRepetitionComponent sr)
    {
        _db = db;
        _sr = sr;
    }

    private Question MapQuestion(Dictionary<string, object?> row)
    {
        return new Question
        {
            Id = Convert.ToInt32(row["id"]),
            SubjectId = row["subject_id"] != null ? Convert.ToInt32(row["subject_id"]) : null,
            Topic = row["topic"]?.ToString() ?? "",
            Difficulty = row["difficulty"]?.ToString() ?? "medium",
            Stem = row["stem"]?.ToString() ?? "",
            OptionsJson = row["options_json"]?.ToString() ?? "[]",
            Answer = row["answer"]?.ToString() ?? "",
            SolutionMd = row["solution_md"]?.ToString() ?? "",
            Source = row["source"]?.ToString() ?? "GATE",
            TagsJson = row["tags_json"]?.ToString() ?? "[]",
            SrEase = row["sr_ease"] != null ? Convert.ToDouble(row["sr_ease"]) : 2.5,
            SrInterval = row["sr_interval"] != null ? Convert.ToInt32(row["sr_interval"]) : 0,
            SrDue = row["sr_due"]?.ToString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            SrReps = row["sr_reps"] != null ? Convert.ToInt32(row["sr_reps"]) : 0,
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
        };
    }

    public List<Question> GetQuestions(string? topic = null, bool dueOnly = false, int limit = 100)
    {
        var sb = new StringBuilder("SELECT * FROM questions WHERE 1=1");
        var parameters = new List<(string Name, object? Value)>();

        if (dueOnly)
        {
            sb.Append(" AND sr_due <= DATE('now')");
        }

        if (!string.IsNullOrEmpty(topic))
        {
            sb.Append(" AND topic = @topic");
            parameters.Add(("@topic", topic));
        }

        sb.Append(" ORDER BY sr_due ASC, id ASC LIMIT @limit");
        parameters.Add(("@limit", limit));

        var rows = _db.Query(sb.ToString(), parameters.ToArray());
        var list = new List<Question>();
        foreach (var r in rows)
        {
            list.Add(MapQuestion(r));
        }
        return list;
    }

    public Question? GetQuestionById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM questions WHERE id = @id", ("@id", id));
        return row != null ? MapQuestion(row) : null;
    }

    public Attempt LogAttempt(int questionId, AttemptCreateRequest request)
    {
        var question = GetQuestionById(questionId);
        if (question == null)
            throw new ArgumentException($"Question {questionId} does not exist.");

        // Compare answer (letter or option text match)
        bool isCorrect = string.Equals(question.Answer.Trim(), request.UserAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

        // Update SM-2 parameters
        var (newEase, newInterval, newReps, nextDueDate) = _sr.CalculateNextReview(
            question.SrEase, question.SrInterval, question.SrReps, isCorrect);

        _db.ExecuteNonQuery(@"
            UPDATE questions 
            SET sr_ease = @ease, sr_interval = @interval, sr_reps = @reps, sr_due = @due
            WHERE id = @id
        ",
            ("@ease", newEase),
            ("@interval", newInterval),
            ("@reps", newReps),
            ("@due", nextDueDate),
            ("@id", questionId)
        );

        // Insert attempt
        long attemptId = _db.InsertAndGetId(@"
            INSERT INTO attempts (question_id, user_answer, correct, time_sec, confidence, notes, created_at)
            VALUES (@qId, @ans, @correct, @timeSec, @confidence, @notes, datetime('now'))
        ",
            ("@qId", questionId),
            ("@ans", request.UserAnswer),
            ("@correct", isCorrect),
            ("@timeSec", request.TimeSec),
            ("@confidence", request.Confidence),
            ("@notes", request.Notes)
        );

        return new Attempt
        {
            Id = (int)attemptId,
            QuestionId = questionId,
            UserAnswer = request.UserAnswer,
            Correct = isCorrect,
            TimeSec = request.TimeSec,
            Confidence = request.Confidence,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public List<Attempt> GetRecentAttempts(int limit = 10)
    {
        var rows = _db.Query(@"
            SELECT * FROM attempts ORDER BY created_at DESC LIMIT @limit
        ", ("@limit", limit));

        var list = new List<Attempt>();
        foreach (var r in rows)
        {
            list.Add(new Attempt
            {
                Id = Convert.ToInt32(r["id"]),
                QuestionId = Convert.ToInt32(r["question_id"]),
                UserAnswer = r["user_answer"]?.ToString() ?? "",
                Correct = Convert.ToInt32(r["correct"]) == 1,
                TimeSec = Convert.ToInt32(r["time_sec"]),
                Confidence = Convert.ToInt32(r["confidence"]),
                Notes = r["notes"]?.ToString(),
                CreatedAt = DateTime.TryParse(r["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
            });
        }
        return list;
    }
}
