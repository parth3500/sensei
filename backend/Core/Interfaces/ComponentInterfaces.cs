using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sensei.Core.Models;

namespace Sensei.Core.Interfaces;

public interface IDatabaseComponent
{
    void Initialize();
    void ExecuteScript(string sqlScript);
    int ExecuteNonQuery(string sql, params (string Name, object? Value)[] parameters);
    long InsertAndGetId(string sql, params (string Name, object? Value)[] parameters);
    List<Dictionary<string, object?>> Query(string sql, params (string Name, object? Value)[] parameters);
    Dictionary<string, object?>? QuerySingle(string sql, params (string Name, object? Value)[] parameters);
    T? QueryScalar<T>(string sql, params (string Name, object? Value)[] parameters);
}

public interface IAuthComponent
{
    bool VerifyPassphrase(string input);
    string HashPassphrase(string phrase);
    string GetConfiguredPassphrase();
}

public interface ITaskComponent
{
    List<TaskItem> GetTasks(string? status = null, string? date = null, int? subjectId = null);
    TaskItem? GetTaskById(int id);
    TaskItem CreateTask(TaskCreateRequest request);
    TaskItem? UpdateTask(int id, TaskUpdateRequest request);
    bool DeleteTask(int id);
}

public interface ISpacedRepetitionComponent
{
    (double NewEase, int NewInterval, int NewReps, string NextDueDate) CalculateNextReview(
        double currentEase, int currentInterval, int currentReps, bool isCorrect, int confidence = 3);
}

public interface IPracticeComponent
{
    List<Question> GetQuestions(string? topic = null, bool dueOnly = false, int limit = 50);
    Question? GetQuestionById(int id);
    Attempt LogAttempt(int questionId, AttemptCreateRequest request);
    List<Attempt> GetRecentAttempts(int limit = 10);
}

public interface IAnalyticsComponent
{
    StatsOverviewResponse GetOverview();
    HeatmapResponse GetHeatmap(int days = 90);
    WeakTopicsResponse GetWeakTopics(int limit = 5);
    AdvancedAnalyticsResponse GetAdvancedAnalytics();
    List<ForgettingRiskQuestion> GetForgettingRiskQuestions(int limit = 5);
    void ResetAllMetrics();
}

public interface IAiTutorComponent
{
    Task<ChatResponse> ChatAsync(ChatRequest request);
    Task<List<Question>> GenerateQuestionsAsync(GenerateQuestionsRequest request);
    Task<object> GenerateStudyPlanAsync(StudyPlanRequest request);
    List<AiMessage> GetChatHistory(int limit = 50);
}

public interface IRemindersComponent
{
    List<Reminder> GetReminders();
    Reminder? UpdateReminder(int id, ReminderUpdateRequest request);
    bool SavePushSubscription(PushSubscription subscription);
}

public interface IVideoLectureComponent
{
    List<LectureTracker> GetLectures();
    LectureTracker? GetLectureById(int id);
    LectureTracker CreateLecture(CreateLectureRequest request);
    LectureTracker? UpdateProgress(int id, UpdateLectureProgressRequest request);
    bool DeleteLecture(int id);
    YouTubeSyncResult SyncYouTubeHistory(YouTubeSyncRequest request);
}

public interface IGoogleDriveStorageComponent
{
    Task<DriveProcessResult> IngestDriveFileAsync(ProcessDriveFileRequest request);
    List<DriveFile> GetIngestedFiles();
    Task<string> BackupDatabaseToDriveAsync();
    object GetDriveStatus();
    Task<DriveSyncResult> SyncFromGoogleDriveAsync();
}

public interface IModuleManagerComponent
{
    List<StudyModule> GetModules();
    StudyModule RegisterModule(RegisterModuleRequest request);
    bool ToggleModule(int id, bool enabled);
}

public interface IMockTestComponent
{
    MockTestStartResponse StartMockTest(StartMockTestRequest request);
    MockTestResultResponse SubmitMockTest(string sessionId, SubmitMockTestRequest request);
    MockTestResultResponse? GetSessionResult(string sessionId);
    List<MockTestHistoryItem> GetHistory(int limit = 20);
}

public interface IFormulaVaultComponent
{
    List<FormulaItem> GetFormulas(string? category = null, string? search = null);
    FormulaItem? GetFormulaById(int id);
    FormulaItem CreateFormula(CreateFormulaRequest request);
    bool DeleteFormula(int id);
    List<string> GetCategories();
}

public interface IWeightageAnalysisComponent
{
    WeightageOverviewDto GetOverview();
    List<HighYieldTopicDto> GetHighYieldTopics(string? subject = null, string? priority = null);
    HighYieldTopicDto? ToggleChecklistItem(int id, bool? isCompleted = null);
    HighYieldTopicDto? UpdateChecklistNotes(int id, string notes);
    WeightedPaperResponse GenerateWeightedPaper(GenerateWeightedPaperRequest request);
    List<WeightedPaperSummaryDto> GetGeneratedPapers(int limit = 20);
    WeightedPaperResponse? GetPaperById(string paperId);
}

public interface IFlashcardComponent
{
    List<FlashcardDeck> GetDecks();
    FlashcardDeck? GetDeckById(int id);
    FlashcardDeck CreateDeck(CreateDeckRequest request);
    bool DeleteDeck(int id);

    List<FlashcardItem> GetCardsInDeck(int deckId, bool dueOnly = false);
    FlashcardItem? GetCardById(int id);
    FlashcardItem AddCard(CreateFlashcardRequest request);
    FlashcardItem? ReviewCard(int id, ReviewFlashcardRequest request);
    bool DeleteCard(int id);
    FlashcardDeckStats GetDeckStats(int deckId);
}



