using System.Collections.Generic;

namespace Sensei.Core.Models;

public record LoginRequest(string Passphrase);
public record AuthResponse(bool Authenticated, string Message);

public record TaskCreateRequest(
    string Title,
    int? SubjectId = null,
    string? Topic = null,
    string? DueDate = null,
    string Priority = "medium",
    int? EstMin = null
);

public record TaskUpdateRequest(
    string? Title = null,
    string? Status = null,
    string? Priority = null,
    string? DueDate = null,
    int? ActualMin = null
);

public record AttemptCreateRequest(
    string UserAnswer,
    int TimeSec = 10,
    int Confidence = 3,
    string? Notes = null
);

public record StartSessionRequest(int? TaskId);
public record EndSessionRequest(int DurationMin = 0, string? Mood = null, int? FocusScore = null, string? Notes = null);

public record ChatMessageDto(string Role, string Content);
public record ChatRequest(List<ChatMessageDto> Messages, string? Model = null);
public record ChatResponse(string Role, string Content);

public record GenerateQuestionsRequest(string Topic, int Count = 5, string? Model = null);
public record StudyPlanRequest(int Days = 30, string? ExamDate = null, string? Model = null);

public record StatsOverviewResponse(
    int Streak,
    int ExamCountdownDays,
    int TodayTasksTotal,
    int TodayTasksDone,
    double Accuracy
);

public record HeatmapItem(string Date, int Count);
public record HeatmapResponse(List<HeatmapItem> Data);

public record WeakTopicItem(string Topic, int TotalAttempts, int Mistakes, double Accuracy);
public record WeakTopicsResponse(List<WeakTopicItem> WeakTopics);

public record ReminderUpdateRequest(bool? Enabled, string? Cron, string? Title);

public record CreateLectureRequest(string Title, string VideoUrl, int? TotalDurationSec = null, int? SubjectId = null);
public record UpdateLectureProgressRequest(int CurrentTimeSec, int? TotalDurationSec = null, bool? Completed = null, string? Notes = null);

public record ProcessDriveFileRequest(string FileName, string Content, string? FolderPath = null);
public record DriveProcessResult(string FileName, string Summary, List<string> RevisionPoints, List<Question> GeneratedQuestions);

public record RegisterModuleRequest(string ModuleKey, string Title, string? Author = null, string? Description = null, string Icon = "Layers");

public record ForgettingRiskQuestion(Question Question, double RetentionProbability, int DaysSinceReview, string RiskLevel);
public record SubjectMastery(string Subject, int TotalQuestions, int Mastered, double MasteryPercentage);
public record TimeSpentMetric(string Category, int Minutes);

public record AdvancedAnalyticsResponse(
    double OverallAccuracy,
    double RetentionRateIndex,
    List<SubjectMastery> SubjectMastery,
    List<TimeSpentMetric> TimeSpent,
    List<ForgettingRiskQuestion> HighRiskTopics
);

public class StartMockTestRequest
{
    public int? QuestionCount { get; set; }
    public int? Count { get; set; }
    public string? Mode { get; set; } // "quick", "subject", "full"
    public string? Subject { get; set; }
    public int? DurationMinutes { get; set; }
}

public record MockTestQuestionDto(
    int Id,
    string Topic,
    string Difficulty,
    string Stem,
    List<string> Options
);

public record MockTestStartResponse(
    string SessionId,
    string Title,
    int TotalQuestions,
    int DurationMinutes,
    DateTime StartedAt,
    List<MockTestQuestionDto> Questions
);

public class MockTestAnswerSubmission
{
    public int QuestionId { get; set; }
    public string? UserAnswer { get; set; }
    public bool IsMarkedForReview { get; set; } = false;
    public int TimeSpentSec { get; set; } = 0;
}

public class SubmitMockTestRequest
{
    public List<MockTestAnswerSubmission>? Answers { get; set; }
    public Dictionary<string, string>? AnswerMap { get; set; }
    public int TotalTimeSpentSec { get; set; } = 0;
}

public record QuestionReviewItem(
    int QuestionId,
    string Topic,
    string Stem,
    List<string> Options,
    string? UserAnswer,
    string CorrectAnswer,
    bool IsCorrect,
    bool IsAttempted,
    bool IsMarkedForReview,
    int TimeSpentSec,
    double MarksAwarded,
    string SolutionMd
);

public record SubjectPerformanceItem(
    string Topic,
    int Total,
    int Correct,
    int Wrong,
    int Unattempted,
    double Marks,
    double Accuracy
);

public record MockTestResultResponse(
    string SessionId,
    string Title,
    int TotalQuestions,
    int AttemptedCount,
    int CorrectCount,
    int WrongCount,
    int UnattemptedCount,
    double PositiveMarks,
    double NegativeMarks,
    double TotalScore,
    double Percentage,
    double Accuracy,
    int TotalTimeSpentSec,
    int AvgTimePerQuestionSec,
    double PercentileEstimate,
    string RankEstimate,
    List<SubjectPerformanceItem> SubjectBreakdown,
    List<QuestionReviewItem> QuestionsReview
);

public record MockTestHistoryItem(
    string SessionId,
    string Title,
    int TotalQuestions,
    double TotalScore,
    double Percentage,
    double Accuracy,
    double PercentileEstimate,
    int TotalTimeSpentSec,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    string Status
);

public record CreateFormulaRequest(
    string Category,
    string Title,
    string Formula,
    string? Description = null,
    string? KeyVariables = null,
    string? Example = null
);

// ── Weightage & Syllabus Analytics DTOs ────────────────────

public record SubjectWeightageItem(
    string SubjectKey,
    string SubjectName,
    double WeightagePercent,
    double TargetHours,
    double ActualHours,
    int AttemptedCount,
    int CorrectCount,
    double Accuracy,
    double MasteryPercentage,
    string Color,
    string Status,
    List<string> HighYieldSummary
);

public record HighYieldSummaryDto(
    int TotalTopics,
    int CompletedTopics,
    int CriticalPendingCount,
    double CompletionPercentage
);

public record WeightageOverviewDto(
    List<SubjectWeightageItem> Subjects,
    double TotalTargetHours,
    double TotalActualHours,
    double OverallMastery,
    HighYieldSummaryDto HighYieldSummary
);

public record HighYieldTopicDto(
    int Id,
    string SubjectKey,
    string SubjectName,
    string TopicName,
    string Priority,
    int FrequencyScore,
    double AvgMarks,
    string RecurrencePattern,
    string KeyFormulaOrConcept,
    bool IsCompleted,
    string? Notes
);

public record UpdateChecklistNotesRequest(
    string Notes
);

public record GenerateWeightedPaperRequest(
    int QuestionCount = 25,
    int? DurationMinutes = null,
    string? Title = null,
    string? Difficulty = null
);

public record PaperSubjectAllocation(
    string SubjectKey,
    string SubjectName,
    double TargetPercent,
    int QuestionCount,
    string Color
);

public record WeightedPaperQuestionDto(
    int Id,
    string SubjectKey,
    string SubjectName,
    string Topic,
    string Difficulty,
    string Stem,
    List<string> Options,
    string? CorrectAnswer = null,
    string? SolutionMd = null
);

public record WeightedPaperResponse(
    string PaperId,
    string Title,
    int TotalQuestions,
    int DurationMinutes,
    List<PaperSubjectAllocation> SubjectAllocations,
    List<WeightedPaperQuestionDto> Questions,
    DateTime CreatedAt
);

public record WeightedPaperSummaryDto(
    string PaperId,
    string Title,
    int TotalQuestions,
    int DurationMinutes,
    DateTime CreatedAt
);

// ── Flashcard & Leitner Box Spaced Repetition DTOs ────────
public record CreateDeckRequest(
    string Title,
    string? Description = null,
    string? Subject = null,
    string? Color = null,
    string? Icon = null
);

public record CreateFlashcardRequest(
    int DeckId,
    string Front,
    string Back,
    string? Notes = null
);

public record ReviewFlashcardRequest(
    string Rating
);

public record FlashcardDeckStats(
    int TotalCards,
    int DueCards,
    int Box1Count,
    int Box2Count,
    int Box3Count,
    int Box4Count,
    int Box5Count
);



