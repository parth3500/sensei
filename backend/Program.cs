using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sensei.Components;
using Sensei.Core.Interfaces;
using Sensei.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment (Railway/Docker) or default to 5000
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configure JSON serialization (camelCase)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Configure CORS for Next.js frontend and production deployments
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Component Assembly Registration
string dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "sensei.db");
builder.Services.AddSingleton<IDatabaseComponent>(_ => new SqliteDatabaseComponent(dbPath));
builder.Services.AddSingleton<IAuthComponent, AuthComponent>();
builder.Services.AddSingleton<ISpacedRepetitionComponent, SpacedRepetitionComponent>();
builder.Services.AddScoped<ITaskComponent, TaskComponent>();
builder.Services.AddScoped<IPracticeComponent, PracticeComponent>();
builder.Services.AddScoped<IAnalyticsComponent, AnalyticsComponent>();
builder.Services.AddScoped<IAiTutorComponent, AiTutorComponent>();
builder.Services.AddScoped<IRemindersComponent, RemindersComponent>();
builder.Services.AddScoped<IVideoLectureComponent, VideoLectureComponent>();
builder.Services.AddScoped<IGoogleDriveStorageComponent, GoogleDriveStorageComponent>();
builder.Services.AddScoped<IModuleManagerComponent, ModuleManagerComponent>();
builder.Services.AddScoped<IMockTestComponent, MockTestComponent>();
builder.Services.AddScoped<IFormulaVaultComponent, FormulaVaultComponent>();
builder.Services.AddScoped<IWeightageAnalysisComponent, WeightageAnalysisComponent>();
builder.Services.AddScoped<IFlashcardComponent, FlashcardComponent>();

var app = builder.Build();




app.UseCors();

// Initialize & Seed Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDatabaseComponent>();
    db.Initialize();
    DatabaseSeeder.Seed(db);
}

// Map Endpoints
app.MapSenseiEndpoints();

Console.WriteLine($"Sensei C# (.NET 8) Web API running on http://0.0.0.0:{port}");

app.Run();
