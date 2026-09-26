using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            sb.Append(" AND (sr_due IS NULL OR sr_due = '' OR DATE(sr_due) <= DATE('now'))");
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

        // Compare answer (letter, stripped text, or exact match)
        string expected = question.Answer.Trim();
        string actual = (request.UserAnswer ?? "").Trim();
        bool isCorrect = string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);

        if (!isCorrect)
        {
            try
            {
                var options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson ?? "[]");
                if (options != null && options.Count > 0)
                {
                    // If expected is A, B, C, D...
                    if (expected.Length == 1 && char.IsLetter(expected[0]))
                    {
                        int idx = char.ToUpperInvariant(expected[0]) - 'A';
                        if (idx >= 0 && idx < options.Count)
                        {
                            if (string.Equals(options[idx].Trim(), actual, StringComparison.OrdinalIgnoreCase))
                                isCorrect = true;
                        }
                    }

                    // If actual is A, B, C, D...
                    if (!isCorrect && actual.Length == 1 && char.IsLetter(actual[0]))
                    {
                        int idx = char.ToUpperInvariant(actual[0]) - 'A';
                        if (idx >= 0 && idx < options.Count)
                        {
                            if (string.Equals(options[idx].Trim(), expected, StringComparison.OrdinalIgnoreCase))
                                isCorrect = true;
                        }
                    }

                    // If option has prefix like "A) " or "1. "
                    if (!isCorrect)
                    {
                        string Clean(string s) => Regex.Replace(s, @"^[A-Da-d0-9][\)\.\:\-]\s*", "").Trim();
                        if (string.Equals(Clean(expected), Clean(actual), StringComparison.OrdinalIgnoreCase))
                            isCorrect = true;
                    }
                }
            }
            catch
            {
                // Fallback to strict comparison
            }
        }

        // Update SM-2 parameters factoring in user confidence
        var (newEase, newInterval, newReps, nextDueDate) = _sr.CalculateNextReview(
            question.SrEase, question.SrInterval, question.SrReps, isCorrect, request.Confidence);

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
