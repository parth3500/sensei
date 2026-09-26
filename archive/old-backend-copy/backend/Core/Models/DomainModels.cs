using System;
using System.Collections.Generic;

namespace Sensei.Core.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public double Weight { get; set; } = 1.0;
    public string Color { get; set; } = "#7c5cff";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? SubjectId { get; set; }
    public string? Topic { get; set; }
    public string? DueDate { get; set; }
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "pending";
    public int? EstMin { get; set; }
    public int ActualMin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DoneAt { get; set; }
}

public class Question
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public string Stem { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = "[]";
    public string Answer { get; set; } = string.Empty;
    public string SolutionMd { get; set; } = string.Empty;
    public string Source { get; set; } = "GATE";
    public string TagsJson { get; set; } = "[]";
    public double SrEase { get; set; } = 2.5;
    public int SrInterval { get; set; } = 0;
    public string SrDue { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public int SrReps { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Attempt
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public bool Correct { get; set; }
    public int TimeSec { get; set; }
    public int Confidence { get; set; } = 3;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StudySession
{
    public int Id { get; set; }
    public int? TaskId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int DurationMin { get; set; }
    public string? Mood { get; set; }
    public int? FocusScore { get; set; }
    public string? Notes { get; set; }
}

public class AiMessage
{
    public int Id { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? ContextJson { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public string Model { get; set; } = "default";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Reminder
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string Channel { get; set; } = "webpush";
    public string PayloadJson { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
    public DateTime? LastFired { get; set; }
}

public class PushSubscription
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Ua { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LectureTracker
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? VideoId { get; set; }
    public int CurrentTimeSec { get; set; }
    public int TotalDurationSec { get; set; }
    public bool Completed { get; set; }
    public string? Notes { get; set; }
    public int? SubjectId { get; set; }
    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DriveFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileId { get; set; }
    public string? FolderPath { get; set; }
    public string? FileType { get; set; }
    public string? ExtractedSummary { get; set; }
    public string? RevisionNotes { get; set; }
    public int GeneratedQuestionsCount { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class StudyModule
{
    public int Id { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string Icon { get; set; } = "Layers";
    public bool Enabled { get; set; } = true;
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FormulaItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? KeyVariables { get; set; }
    public string? Example { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class MockTestSession
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int DurationMin { get; set; }
    public string QuestionsJson { get; set; } = "[]";
    public string Status { get; set; } = "in_progress"; // "in_progress", "completed", "expired"
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int TimeSpentSec { get; set; }
    public double Score { get; set; }
    public double PositiveMarks { get; set; }
    public double NegativeMarks { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int UnattemptedCount { get; set; }
    public double Accuracy { get; set; }
    public double PercentileEstimate { get; set; }
    public string? SubmissionJson { get; set; }
}

public class FlashcardDeck
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Subject { get; set; }
    public string Color { get; set; } = "#7c5cff";
    public string Icon { get; set; } = "Layers";
    public int CardCount { get; set; }
    public int DueCount { get; set; }
    public Dictionary<int, int> BoxCounts { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FlashcardItem
{
    public int Id { get; set; }
    public int DeckId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int Box { get; set; } = 1;
    public string NextReviewDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public int Reps { get; set; } = 0;
    public double Ease { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 1;
    public DateTime? LastReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



