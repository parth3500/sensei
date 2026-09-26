using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class MockTestComponent : IMockTestComponent
{
    private readonly IDatabaseComponent _db;
    private readonly ISpacedRepetitionComponent _sr;

    public MockTestComponent(IDatabaseComponent db, ISpacedRepetitionComponent sr)
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

    public MockTestStartResponse StartMockTest(StartMockTestRequest request)
    {
        // 1. Determine Question Count
        int count = 10;
        if (!string.IsNullOrEmpty(request.Mode))
        {
            count = request.Mode.ToLowerInvariant() switch
            {
                "quick" => 10,
                "subject" => 25,
                "full" => 65,
                _ => 10
            };
        }
        else if (request.QuestionCount.HasValue && request.QuestionCount.Value > 0)
        {
            count = request.QuestionCount.Value;
        }
        else if (request.Count.HasValue && request.Count.Value > 0)
        {
            count = request.Count.Value;
        }

        // 2. Determine Duration
        int durationMinutes;
        if (request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0)
        {
            durationMinutes = request.DurationMinutes.Value;
        }
        else if (count <= 10)
        {
            durationMinutes = 15;
        }
        else if (count <= 25)
        {
            durationMinutes = 45;
        }
        else
        {
            durationMinutes = 90;
        }

        // 3. Determine Title
        string title = count switch
        {
            <= 10 => "GATE CS Quick Sprint (10 Qs)",
            <= 25 => "GATE CS Subject Test (25 Qs)",
            _ => $"GATE CS Full Mock Exam ({count} Qs)"
        };

        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            title = $"{request.Subject} Test ({count} Qs)";
        }

        // 4. Query Questions from Database
        List<Dictionary<string, object?>> rows;
        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            rows = _db.Query(
                "SELECT * FROM questions WHERE topic LIKE @subj ORDER BY RANDOM() LIMIT @limit",
                ("@subj", $"%{request.Subject}%"),
                ("@limit", count)
            );

            // Fallback if subject has fewer questions
            if (rows.Count < count)
            {
                var additionalRows = _db.Query(
                    "SELECT * FROM questions WHERE id NOT IN (" + string.Join(",", rows.Select(r => r["id"])) + ") ORDER BY RANDOM() LIMIT @limit",
                    ("@limit", count - rows.Count)
                );
                rows.AddRange(additionalRows);
            }
        }
        else
        {
            rows = _db.Query("SELECT * FROM questions ORDER BY RANDOM() LIMIT @limit", ("@limit", count));
        }

        var questions = rows.Select(MapQuestion).ToList();
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No questions available in the database.");
        }

        // 5. Generate Session Token & Persist Session
        string sessionId = Guid.NewGuid().ToString("N");
        string questionsJson = JsonSerializer.Serialize(questions);

        _db.ExecuteNonQuery(@"
            INSERT INTO mock_test_sessions (
                id, title, question_count, duration_min, questions_json, status, started_at
            ) VALUES (
                @id, @title, @qCount, @dur, @qJson, 'in_progress', datetime('now')
            )
        ",
            ("@id", sessionId),
            ("@title", title),
            ("@qCount", questions.Count),
            ("@dur", durationMinutes),
            ("@qJson", questionsJson)
        );

        // 6. Map to client-safe DTOs (strip answer and solutionMd)
        var clientQuestions = questions.Select(q =>
        {
            List<string> options;
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>();
            }
            catch
            {
                options = new List<string>();
            }

            return new MockTestQuestionDto(
                Id: q.Id,
                Topic: q.Topic,
                Difficulty: q.Difficulty,
                Stem: q.Stem,
                Options: options
            );
        }).ToList();

        return new MockTestStartResponse(
            SessionId: sessionId,
            Title: title,
            TotalQuestions: clientQuestions.Count,
            DurationMinutes: durationMinutes,
            StartedAt: DateTime.UtcNow,
            Questions: clientQuestions
        );
    }

    public MockTestResultResponse SubmitMockTest(string sessionId, SubmitMockTestRequest request)
    {
        var sessionRow = _db.QuerySingle("SELECT * FROM mock_test_sessions WHERE id = @id", ("@id", sessionId));
        if (sessionRow == null)
        {
            throw new KeyNotFoundException($"Mock test session '{sessionId}' not found.");
        }

        // If already completed, return existing evaluation report
        string? existingSubmission = sessionRow["submission_json"]?.ToString();
        if (sessionRow["status"]?.ToString() == "completed" && !string.IsNullOrWhiteSpace(existingSubmission))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<MockTestResultResponse>(existingSubmission, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (existing != null) return existing;
            }
            catch
            {
                // recompute if deserialization fails
            }
        }

        // Deserialize questions with correct answers
        string questionsJson = sessionRow["questions_json"]?.ToString() ?? "[]";
        var questions = JsonSerializer.Deserialize<List<Question>>(questionsJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Question>();

        int totalQuestions = questions.Count;
        string title = sessionRow["title"]?.ToString() ?? "GATE Mock Test";

        // Map incoming answers
        var answerMap = new Dictionary<int, MockTestAnswerSubmission>();
        if (request.Answers != null)
        {
            foreach (var a in request.Answers)
            {
                answerMap[a.QuestionId] = a;
            }
        }
        if (request.AnswerMap != null)
        {
            foreach (var (k, v) in request.AnswerMap)
            {
                if (int.TryParse(k, out int qId) && !answerMap.ContainsKey(qId))
                {
                    answerMap[qId] = new MockTestAnswerSubmission { QuestionId = qId, UserAnswer = v };
                }
            }
        }

        int correctCount = 0;
        int wrongCount = 0;
        int unattemptedCount = 0;
        double positiveMarks = 0.0;
        double negativeMarks = 0.0;

        var reviews = new List<QuestionReviewItem>();

        foreach (var q in questions)
        {
            answerMap.TryGetValue(q.Id, out var sub);
            string? userAns = sub?.UserAnswer?.Trim();
            bool isAttempted = !string.IsNullOrEmpty(userAns);
            bool isMarked = sub?.IsMarkedForReview ?? false;
            int timeSpent = sub?.TimeSpentSec ?? 0;

            List<string> options;
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>();
            }
            catch
            {
                options = new List<string>();
            }

            bool isCorrect = false;
            double marksAwarded = 0.0;

            if (!isAttempted)
            {
                unattemptedCount++;
                marksAwarded = 0.0;
            }
            else
            {
                // GATE marking: +1 for correct, -0.33 for wrong
                isCorrect = string.Equals(userAns, q.Answer.Trim(), StringComparison.OrdinalIgnoreCase);
                if (isCorrect)
                {
                    correctCount++;
                    positiveMarks += 1.0;
                    marksAwarded = 1.0;
                }
                else
                {
                    wrongCount++;
                    negativeMarks += 0.33;
                    marksAwarded = -0.33;
                }

                // Update SM-2 parameters & record attempt for spaced repetition analytics
                try
                {
                    var (newEase, newInterval, newReps, nextDueDate) = _sr.CalculateNextReview(
                        q.SrEase, q.SrInterval, q.SrReps, isCorrect);

                    _db.ExecuteNonQuery(@"
                        UPDATE questions 
                        SET sr_ease = @ease, sr_interval = @interval, sr_reps = @reps, sr_due = @due
                        WHERE id = @id
                    ",
                        ("@ease", newEase),
                        ("@interval", newInterval),
                        ("@reps", newReps),
                        ("@due", nextDueDate),
                        ("@id", q.Id)
                    );

                    _db.InsertAndGetId(@"
                        INSERT INTO attempts (question_id, user_answer, correct, time_sec, confidence, notes, created_at)
                        VALUES (@qId, @ans, @correct, @timeSec, 3, @notes, datetime('now'))
                    ",
                        ("@qId", q.Id),
                        ("@ans", userAns!),
                        ("@correct", isCorrect),
                        ("@timeSec", Math.Max(5, timeSpent)),
                        ("@notes", $"Mock Test: {title}")
                    );
                }
                catch
                {
                    // Non-fatal if spaced repetition attempt logging encounters an issue
                }
            }

            reviews.Add(new QuestionReviewItem(
                QuestionId: q.Id,
                Topic: q.Topic,
                Stem: q.Stem,
                Options: options,
                UserAnswer: userAns,
                CorrectAnswer: q.Answer,
                IsCorrect: isCorrect,
                IsAttempted: isAttempted,
                IsMarkedForReview: isMarked,
                TimeSpentSec: timeSpent,
                MarksAwarded: Math.Round(marksAwarded, 2),
                SolutionMd: q.SolutionMd
            ));
        }

        int attemptedCount = correctCount + wrongCount;
        double totalScore = Math.Round(positiveMarks - negativeMarks, 2);
        double percentage = totalQuestions > 0 ? Math.Max(0, Math.Round((totalScore / totalQuestions) * 100, 1)) : 0.0;
        double accuracy = attemptedCount > 0 ? Math.Round(((double)correctCount / attemptedCount) * 100, 1) : 0.0;

        // Subject Breakdown
        var subjectBreakdown = reviews
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Topic) ? "General" : r.Topic)
            .Select(g =>
            {
                int c = g.Count(x => x.IsAttempted && x.IsCorrect);
                int w = g.Count(x => x.IsAttempted && !x.IsCorrect);
                int u = g.Count(x => !x.IsAttempted);
                double m = Math.Round(c * 1.0 - w * 0.33, 2);
                double acc = (c + w) > 0 ? Math.Round(((double)c / (c + w)) * 100, 1) : 0.0;

                return new SubjectPerformanceItem(
                    Topic: g.Key,
                    Total: g.Count(),
                    Correct: c,
                    Wrong: w,
                    Unattempted: u,
                    Marks: m,
                    Accuracy: acc
                );
            })
            .OrderByDescending(s => s.Total)
            .ToList();

        // GATE Percentile and Rank Estimation
        double scorePct = totalQuestions > 0 ? (totalScore / totalQuestions) * 100.0 : 0.0;
        double percentile;
        string rankEstimate;

        if (scorePct >= 80)
        {
            percentile = Math.Min(99.9, 99.0 + (scorePct - 80) * (0.9 / 20.0));
            rankEstimate = "Top 100 (AIR 1 - 150)";
        }
        else if (scorePct >= 65)
        {
            percentile = 97.5 + (scorePct - 65) * (1.5 / 15.0);
            rankEstimate = "Top 500 (AIR 150 - 600)";
        }
        else if (scorePct >= 50)
        {
            percentile = 92.0 + (scorePct - 50) * (5.5 / 15.0);
            rankEstimate = "Top 2,000 (AIR 600 - 2,500)";
        }
        else if (scorePct >= 35)
        {
            percentile = 80.0 + (scorePct - 35) * (12.0 / 15.0);
            rankEstimate = "Top 8,000 (AIR 2,500 - 9,000)";
        }
        else if (scorePct >= 25)
        {
            percentile = 60.0 + (scorePct - 25) * (20.0 / 10.0);
            rankEstimate = "AIR 9,000 - 25,000";
        }
        else if (scorePct >= 10)
        {
            percentile = 30.0 + (scorePct - 10) * (30.0 / 15.0);
            rankEstimate = "AIR 25,000 - 55,000";
        }
        else
        {
            percentile = Math.Max(5.0, scorePct * 3.0);
            rankEstimate = "AIR 55,000+";
        }
        percentile = Math.Round(percentile, 1);

        int totalTimeSpent = request.TotalTimeSpentSec > 0
            ? request.TotalTimeSpentSec
            : reviews.Sum(r => r.TimeSpentSec);
        int avgTimePerQuestion = totalQuestions > 0 ? totalTimeSpent / totalQuestions : 0;

        var result = new MockTestResultResponse(
            SessionId: sessionId,
            Title: title,
            TotalQuestions: totalQuestions,
            AttemptedCount: attemptedCount,
            CorrectCount: correctCount,
            WrongCount: wrongCount,
            UnattemptedCount: unattemptedCount,
            PositiveMarks: Math.Round(positiveMarks, 2),
            NegativeMarks: Math.Round(negativeMarks, 2),
            TotalScore: totalScore,
            Percentage: percentage,
            Accuracy: accuracy,
            TotalTimeSpentSec: totalTimeSpent,
            AvgTimePerQuestionSec: avgTimePerQuestion,
            PercentileEstimate: percentile,
            RankEstimate: rankEstimate,
            SubjectBreakdown: subjectBreakdown,
            QuestionsReview: reviews
        );

        // Update mock_test_sessions record
        string resultJson = JsonSerializer.Serialize(result);
        _db.ExecuteNonQuery(@"
            UPDATE mock_test_sessions
            SET status = 'completed',
                submitted_at = datetime('now'),
                time_spent_sec = @timeSpent,
                score = @score,
                positive_marks = @pos,
                negative_marks = @neg,
                correct_count = @cCount,
                wrong_count = @wCount,
                unattempted_count = @uCount,
                accuracy = @acc,
                percentile_estimate = @pct,
                submission_json = @subJson
            WHERE id = @id
        ",
            ("@timeSpent", totalTimeSpent),
            ("@score", totalScore),
            ("@pos", Math.Round(positiveMarks, 2)),
            ("@neg", Math.Round(negativeMarks, 2)),
            ("@cCount", correctCount),
            ("@wCount", wrongCount),
            ("@uCount", unattemptedCount),
            ("@acc", accuracy),
            ("@pct", percentile),
            ("@subJson", resultJson),
            ("@id", sessionId)
        );

        return result;
    }

    public MockTestResultResponse? GetSessionResult(string sessionId)
    {
        var row = _db.QuerySingle("SELECT submission_json FROM mock_test_sessions WHERE id = @id", ("@id", sessionId));
        if (row == null || row["submission_json"] == null) return null;

        string subJson = row["submission_json"]!.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(subJson)) return null;

        try
        {
            return JsonSerializer.Deserialize<MockTestResultResponse>(subJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    public List<MockTestHistoryItem> GetHistory(int limit = 20)
    {
        var rows = _db.Query(@"
            SELECT id, title, question_count, score, accuracy, percentile_estimate, 
                   time_spent_sec, started_at, submitted_at, status
            FROM mock_test_sessions
            ORDER BY started_at DESC
            LIMIT @limit
        ", ("@limit", limit));

        var list = new List<MockTestHistoryItem>();
        foreach (var r in rows)
        {
            int qCount = Convert.ToInt32(r["question_count"]);
            double score = r["score"] != null ? Convert.ToDouble(r["score"]) : 0.0;
            double pct = qCount > 0 ? Math.Max(0, Math.Round((score / qCount) * 100, 1)) : 0.0;

            list.Add(new MockTestHistoryItem(
                SessionId: r["id"]?.ToString() ?? "",
                Title: r["title"]?.ToString() ?? "Mock Test",
                TotalQuestions: qCount,
                TotalScore: score,
                Percentage: pct,
                Accuracy: r["accuracy"] != null ? Convert.ToDouble(r["accuracy"]) : 0.0,
                PercentileEstimate: r["percentile_estimate"] != null ? Convert.ToDouble(r["percentile_estimate"]) : 0.0,
                TotalTimeSpentSec: r["time_spent_sec"] != null ? Convert.ToInt32(r["time_spent_sec"]) : 0,
                StartedAt: DateTime.TryParse(r["started_at"]?.ToString(), out var sdt) ? sdt : DateTime.UtcNow,
                SubmittedAt: DateTime.TryParse(r["submitted_at"]?.ToString(), out var subdt) ? subdt : null,
                Status: r["status"]?.ToString() ?? "completed"
            ));
        }
        return list;
    }
}
