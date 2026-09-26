using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Endpoints;

public static class ApiEndpoints
{
    public static void MapSenseiEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Auth Endpoints ─────────────────────────────────────
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", (LoginRequest req, IAuthComponent authComp, HttpContext ctx) =>
        {
            if (authComp.VerifyPassphrase(req.Passphrase))
            {
                ctx.Response.Cookies.Append("auth", "authenticated", new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = false, // Allow local testing over http
                    Expires = DateTimeOffset.UtcNow.AddDays(60)
                });
                return Results.Ok(new AuthResponse(true, "Logged in"));
            }
            return Results.Json(new AuthResponse(false, "Invalid passphrase"), statusCode: 401);
        });

        auth.MapPost("/logout", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete("auth");
            return Results.Ok(new AuthResponse(false, "Logged out"));
        });

        auth.MapGet("/me", (HttpContext ctx) =>
        {
            bool isAuth = ctx.Request.Cookies["auth"] == "authenticated";
            return Results.Ok(new AuthResponse(isAuth, isAuth ? "Authenticated" : "Not authenticated"));
        });

        // ── Tasks Endpoints ────────────────────────────────────
        var tasks = app.MapGroup("/api/tasks").WithTags("Tasks");

        tasks.MapGet("/", (string? status, string? date, int? subjectId, ITaskComponent taskComp) =>
        {
            var list = taskComp.GetTasks(status, date, subjectId);
            return Results.Ok(list);
        });

        tasks.MapGet("/{id:int}", (int id, ITaskComponent taskComp) =>
        {
            var task = taskComp.GetTaskById(id);
            return task != null ? Results.Ok(task) : Results.NotFound();
        });

        tasks.MapPost("/", (TaskCreateRequest req, ITaskComponent taskComp) =>
        {
            var created = taskComp.CreateTask(req);
            return Results.Created($"/api/tasks/{created.Id}", created);
        });

        tasks.MapPatch("/{id:int}", (int id, TaskUpdateRequest req, ITaskComponent taskComp) =>
        {
            var updated = taskComp.UpdateTask(id, req);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        tasks.MapDelete("/{id:int}", (int id, ITaskComponent taskComp) =>
        {
            bool deleted = taskComp.DeleteTask(id);
            return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });

        // ── Questions Endpoints ────────────────────────────────
        var questions = app.MapGroup("/api/questions").WithTags("Questions");

        questions.MapGet("/", (string? topic, bool? dueOnly, int? limit, IPracticeComponent practiceComp) =>
        {
            var list = practiceComp.GetQuestions(topic, dueOnly ?? false, limit ?? 100);
            return Results.Ok(list);
        });

        questions.MapGet("/{id:int}", (int id, IPracticeComponent practiceComp) =>
        {
            var q = practiceComp.GetQuestionById(id);
            return q != null ? Results.Ok(q) : Results.NotFound();
        });

        questions.MapPost("/{id:int}/attempts", (int id, AttemptCreateRequest req, IPracticeComponent practiceComp) =>
        {
            try
            {
                var attempt = practiceComp.LogAttempt(id, req);
                return Results.Ok(attempt);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        questions.MapGet("/attempts/recent", (int? limit, IPracticeComponent practiceComp) =>
        {
            return Results.Ok(practiceComp.GetRecentAttempts(limit ?? 10));
        });

        // ── Stats Endpoints ────────────────────────────────────
        var stats = app.MapGroup("/api/stats").WithTags("Stats");

        stats.MapGet("/overview", (IAnalyticsComponent analytics) =>
        {
            return Results.Ok(analytics.GetOverview());
        });

        stats.MapGet("/heatmap", (int? days, IAnalyticsComponent analytics) =>
        {
            return Results.Ok(analytics.GetHeatmap(days ?? 90));
        });

        stats.MapGet("/weak-topics", (int? limit, IAnalyticsComponent analytics) =>
        {
            return Results.Ok(analytics.GetWeakTopics(limit ?? 5));
        });

        // ── AI Endpoints ───────────────────────────────────────
        var ai = app.MapGroup("/api/ai").WithTags("AI");

        ai.MapPost("/chat", async (ChatRequest req, IAiTutorComponent aiTutor) =>
        {
            var reply = await aiTutor.ChatAsync(req);
            return Results.Ok(reply);
        });

        ai.MapPost("/generate-questions", async (GenerateQuestionsRequest req, IAiTutorComponent aiTutor) =>
        {
            var generated = await aiTutor.GenerateQuestionsAsync(req);
            return Results.Ok(generated);
        });

        ai.MapPost("/study-plan", async (StudyPlanRequest req, IAiTutorComponent aiTutor) =>
        {
            var plan = await aiTutor.GenerateStudyPlanAsync(req);
            return Results.Ok(plan);
        });

        ai.MapGet("/messages", (int? limit, IAiTutorComponent aiTutor) =>
        {
            return Results.Ok(aiTutor.GetChatHistory(limit ?? 50));
        });

        // ── Reminders Endpoints ────────────────────────────────
        var reminders = app.MapGroup("/api/reminders").WithTags("Reminders");

        reminders.MapGet("/", (IRemindersComponent remindersComp) =>
        {
            return Results.Ok(remindersComp.GetReminders());
        });

        reminders.MapPatch("/{id:int}", (int id, ReminderUpdateRequest req, IRemindersComponent remindersComp) =>
        {
            var updated = remindersComp.UpdateReminder(id, req);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        app.MapPost("/api/push/subscribe", (PushSubscription sub, IRemindersComponent remindersComp) =>
        {
            remindersComp.SavePushSubscription(sub);
            return Results.Ok(new { subscribed = true });
        });

        // ── Video Lecture Endpoints ────────────────────────────
        var lectures = app.MapGroup("/api/lectures").WithTags("Lectures");

        lectures.MapGet("/", (IVideoLectureComponent lectureComp) =>
        {
            return Results.Ok(lectureComp.GetLectures());
        });

        lectures.MapGet("/{id:int}", (int id, IVideoLectureComponent lectureComp) =>
        {
            var l = lectureComp.GetLectureById(id);
            return l != null ? Results.Ok(l) : Results.NotFound();
        });

        lectures.MapPost("/", (CreateLectureRequest req, IVideoLectureComponent lectureComp) =>
        {
            var created = lectureComp.CreateLecture(req);
            return Results.Created($"/api/lectures/{created.Id}", created);
        });

        lectures.MapPatch("/{id:int}/progress", (int id, UpdateLectureProgressRequest req, IVideoLectureComponent lectureComp) =>
        {
            var updated = lectureComp.UpdateProgress(id, req);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        lectures.MapDelete("/{id:int}", (int id, IVideoLectureComponent lectureComp) =>
        {
            bool deleted = lectureComp.DeleteLecture(id);
            return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });

        // ── Google Drive & Storage Endpoints ───────────────────
        var drive = app.MapGroup("/api/drive").WithTags("Drive");

        drive.MapGet("/status", (IGoogleDriveStorageComponent driveComp) =>
        {
            return Results.Ok(driveComp.GetDriveStatus());
        });

        drive.MapGet("/files", (IGoogleDriveStorageComponent driveComp) =>
        {
            return Results.Ok(driveComp.GetIngestedFiles());
        });

        drive.MapGet("/sync", async (IGoogleDriveStorageComponent driveComp) =>
        {
            var result = await driveComp.SyncFromGoogleDriveAsync();
            return Results.Ok(result);
        });

        drive.MapPost("/sync", async (IGoogleDriveStorageComponent driveComp) =>
        {
            var result = await driveComp.SyncFromGoogleDriveAsync();
            return Results.Ok(result);
        });

        drive.MapPost("/ingest", async (ProcessDriveFileRequest req, IGoogleDriveStorageComponent driveComp) =>
        {
            var result = await driveComp.IngestDriveFileAsync(req);
            return Results.Ok(result);
        });

        drive.MapPost("/backup", async (IGoogleDriveStorageComponent driveComp) =>
        {
            string path = await driveComp.BackupDatabaseToDriveAsync();
            return Results.Ok(new { success = true, backupPath = path, timestamp = DateTime.UtcNow });
        });

        // ── Pluggable Module Registry Endpoints ────────────────
        var modules = app.MapGroup("/api/modules").WithTags("Modules");

        modules.MapGet("/", (IModuleManagerComponent moduleComp) =>
        {
            return Results.Ok(moduleComp.GetModules());
        });

        modules.MapPost("/", (RegisterModuleRequest req, IModuleManagerComponent moduleComp) =>
        {
            var mod = moduleComp.RegisterModule(req);
            return Results.Created($"/api/modules/{mod.Id}", mod);
        });

        modules.MapPatch("/{id:int}/toggle", (int id, bool enabled, IModuleManagerComponent moduleComp) =>
        {
            bool ok = moduleComp.ToggleModule(id, enabled);
            return ok ? Results.Ok(new { success = true }) : Results.NotFound();
        });

        // ── Formula & Theorem Vault Module Endpoints ──────────
        var formulaVault = app.MapGroup("/api/modules/formula-vault").WithTags("FormulaVault");

        formulaVault.MapGet("/", (string? category, string? search, IFormulaVaultComponent vault) =>
        {
            return Results.Ok(vault.GetFormulas(category, search));
        });

        formulaVault.MapGet("/categories", (IFormulaVaultComponent vault) =>
        {
            return Results.Ok(vault.GetCategories());
        });

        formulaVault.MapGet("/{id:int}", (int id, IFormulaVaultComponent vault) =>
        {
            var item = vault.GetFormulaById(id);
            return item != null ? Results.Ok(item) : Results.NotFound();
        });

        formulaVault.MapPost("/", (CreateFormulaRequest req, IFormulaVaultComponent vault) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Formula))
            {
                return Results.BadRequest(new { error = "Title and Formula are required" });
            }
            var created = vault.CreateFormula(req);
            return Results.Created($"/api/modules/formula-vault/{created.Id}", created);
        });

        formulaVault.MapDelete("/{id:int}", (int id, IFormulaVaultComponent vault) =>
        {
            bool deleted = vault.DeleteFormula(id);
            return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });

        // ── Weightage & Syllabus Analytics Module Endpoints ───
        var weightage = app.MapGroup("/api/modules/weightage-analysis").WithTags("WeightageAnalysis");

        weightage.MapGet("/overview", (IWeightageAnalysisComponent weightageComp) =>
        {
            return Results.Ok(weightageComp.GetOverview());
        });

        weightage.MapGet("/high-yield-topics", (string? subject, string? priority, IWeightageAnalysisComponent weightageComp) =>
        {
            return Results.Ok(weightageComp.GetHighYieldTopics(subject, priority));
        });

        weightage.MapPost("/checklist/{id:int}/toggle", (int id, bool? isCompleted, IWeightageAnalysisComponent weightageComp) =>
        {
            var updated = weightageComp.ToggleChecklistItem(id, isCompleted);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        weightage.MapPost("/checklist/{id:int}/notes", (int id, UpdateChecklistNotesRequest req, IWeightageAnalysisComponent weightageComp) =>
        {
            var updated = weightageComp.UpdateChecklistNotes(id, req.Notes);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        weightage.MapPost("/generate-paper", (GenerateWeightedPaperRequest? req, IWeightageAnalysisComponent weightageComp) =>
        {
            try
            {
                var paper = weightageComp.GenerateWeightedPaper(req ?? new GenerateWeightedPaperRequest());
                return Results.Ok(paper);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        weightage.MapGet("/papers", (int? limit, IWeightageAnalysisComponent weightageComp) =>
        {
            return Results.Ok(weightageComp.GetGeneratedPapers(limit ?? 20));
        });

        weightage.MapGet("/papers/{id}", (string id, IWeightageAnalysisComponent weightageComp) =>
        {
            var paper = weightageComp.GetPaperById(id);
            return paper != null ? Results.Ok(paper) : Results.NotFound(new { error = $"Paper {id} not found" });
        });

        // ── Flashcard & Leitner Box Spaced Repetition Endpoints ──
        var flashcards = app.MapGroup("/api/modules/flashcards").WithTags("Flashcards");

        flashcards.MapGet("/decks", (IFlashcardComponent comp) =>
        {
            return Results.Ok(comp.GetDecks());
        });

        flashcards.MapGet("/decks/{id:int}", (int id, IFlashcardComponent comp) =>
        {
            var deck = comp.GetDeckById(id);
            return deck != null ? Results.Ok(deck) : Results.NotFound();
        });

        flashcards.MapPost("/decks", (CreateDeckRequest req, IFlashcardComponent comp) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return Results.BadRequest(new { error = "Deck title is required" });
            }
            var created = comp.CreateDeck(req);
            return Results.Created($"/api/modules/flashcards/decks/{created.Id}", created);
        });

        flashcards.MapDelete("/decks/{id:int}", (int id, IFlashcardComponent comp) =>
        {
            bool deleted = comp.DeleteDeck(id);
            return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });

        flashcards.MapGet("/decks/{deckId:int}/cards", (int deckId, bool? dueOnly, IFlashcardComponent comp) =>
        {
            var cards = comp.GetCardsInDeck(deckId, dueOnly ?? false);
            return Results.Ok(cards);
        });

        flashcards.MapGet("/decks/{deckId:int}/stats", (int deckId, IFlashcardComponent comp) =>
        {
            var stats = comp.GetDeckStats(deckId);
            return Results.Ok(stats);
        });

        flashcards.MapPost("/cards", (CreateFlashcardRequest req, IFlashcardComponent comp) =>
        {
            if (string.IsNullOrWhiteSpace(req.Front) || string.IsNullOrWhiteSpace(req.Back))
            {
                return Results.BadRequest(new { error = "Front and Back content are required" });
            }
            var card = comp.AddCard(req);
            return Results.Created($"/api/modules/flashcards/cards/{card.Id}", card);
        });

        flashcards.MapPost("/cards/{id:int}/review", (int id, ReviewFlashcardRequest req, IFlashcardComponent comp) =>
        {
            var updated = comp.ReviewCard(id, req);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        });

        flashcards.MapDelete("/cards/{id:int}", (int id, IFlashcardComponent comp) =>
        {
            bool deleted = comp.DeleteCard(id);
            return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
        });



        // ── Advanced Analytics & Forgetting Risk ───────────────
        stats.MapGet("/advanced", (IAnalyticsComponent analytics) =>
        {
            return Results.Ok(analytics.GetAdvancedAnalytics());
        });

        stats.MapGet("/forgetting-risk", (int? limit, IAnalyticsComponent analytics) =>
        {
            return Results.Ok(analytics.GetForgettingRiskQuestions(limit ?? 5));
        });

        // ── Mock Test & Exam Mode Endpoints ────────────────────
        var mockTests = app.MapGroup("/api/mock-tests").WithTags("MockTests");

        mockTests.MapPost("/start", (StartMockTestRequest? req, IMockTestComponent mockComp) =>
        {
            try
            {
                var session = mockComp.StartMockTest(req ?? new StartMockTestRequest());
                return Results.Ok(session);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        mockTests.MapPost("/{sessionId}/submit", (string sessionId, SubmitMockTestRequest req, IMockTestComponent mockComp) =>
        {
            try
            {
                var result = mockComp.SubmitMockTest(sessionId, req);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        mockTests.MapGet("/history", (int? limit, IMockTestComponent mockComp) =>
        {
            var history = mockComp.GetHistory(limit ?? 20);
            return Results.Ok(history);
        });

        mockTests.MapGet("/{sessionId}", (string sessionId, IMockTestComponent mockComp) =>
        {
            var result = mockComp.GetSessionResult(sessionId);
            return result != null ? Results.Ok(result) : Results.NotFound(new { error = $"Session {sessionId} result not found or pending." });
        });

        // ── Health Check ───────────────────────────────────────
        app.MapGet("/health", () => Results.Ok(new { status = "ok", version = "1.0.0", runtime = ".NET 8 Web API" }));
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "1.0.0", runtime = ".NET 8 Web API" }));
    }
}

