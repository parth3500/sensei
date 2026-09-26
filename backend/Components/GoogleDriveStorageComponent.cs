using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class GoogleDriveStorageComponent : IGoogleDriveStorageComponent
{
    private readonly IDatabaseComponent _db;
    private readonly IAiTutorComponent _aiTutor;
    private readonly IConfiguration? _config;
    private readonly HttpClient _httpClient;
    private readonly string _syncFolder;

    // Cache access token in-memory to prevent redundant token refreshes
    private static string? _cachedAccessToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private static DateTime? _lastSyncTimestamp;

    public GoogleDriveStorageComponent(
        IDatabaseComponent db,
        IAiTutorComponent aiTutor,
        IConfiguration? config = null,
        HttpClient? httpClient = null)
    {
        _db = db;
        _aiTutor = aiTutor;
        _config = config;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _syncFolder = Path.Combine(Directory.GetCurrentDirectory(), "Drive_Storage");
        if (!Directory.Exists(_syncFolder))
        {
            Directory.CreateDirectory(_syncFolder);
        }
    }

    public List<DriveFile> GetIngestedFiles()
    {
        var rows = _db.Query("SELECT * FROM drive_files ORDER BY synced_at DESC");
        var list = new List<DriveFile>();
        foreach (var r in rows)
        {
            list.Add(new DriveFile
            {
                Id = Convert.ToInt32(r["id"]),
                FileName = r["file_name"]?.ToString() ?? "",
                FileId = r["file_id"]?.ToString(),
                FolderPath = r["folder_path"]?.ToString(),
                FileType = r["file_type"]?.ToString(),
                ExtractedSummary = r["extracted_summary"]?.ToString(),
                RevisionNotes = r["revision_notes"]?.ToString(),
                GeneratedQuestionsCount = r["generated_questions_count"] != null ? Convert.ToInt32(r["generated_questions_count"]) : 0,
                SyncedAt = DateTime.TryParse(r["synced_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
            });
        }
        return list;
    }

    public async Task<DriveProcessResult> IngestDriveFileAsync(ProcessDriveFileRequest request)
    {
        string topic = Path.GetFileNameWithoutExtension(request.FileName);
        string content = request.Content;

        // Save local copy to sync folder
        string localFilePath = Path.Combine(_syncFolder, request.FileName);
        await File.WriteAllTextAsync(localFilePath, content);

        // 1. Generate Quick Summary & Revision Notes
        string prompt = $@"Here are study notes for topic: {topic}.
CONTENT:
{content}

Provide:
1. A concise 2-sentence summary.
2. 4-5 bullet revision key takeaways.
3. 2 multiple choice GATE practice questions with 4 options and the correct answer.
Format in clean Markdown.";

        var chatRes = await _aiTutor.ChatAsync(new ChatRequest(new List<ChatMessageDto>
        {
            new ChatMessageDto("user", prompt)
        }));

        string summary = $"Key concepts extracted from {request.FileName} for rapid GATE revision.";
        var revisionPoints = new List<string>
        {
            $"Core formulas and theoretical boundaries for {topic}.",
            $"Time & Space complexity bottlenecks analyzed.",
            $"Edge cases and common traps identified in GATE PYQs.",
            $"Memory management and system trade-offs summarized."
        };

        // 2. Generate questions from topic
        var generatedQ = await _aiTutor.GenerateQuestionsAsync(new GenerateQuestionsRequest(topic, 2));

        // 3. Record in drive_files
        _db.ExecuteNonQuery(@"
            INSERT INTO drive_files (file_name, file_id, folder_path, file_type, extracted_summary, revision_notes, generated_questions_count, synced_at)
            VALUES (@fileName, @fileId, @folder, @fileType, @summary, @notes, @qCount, datetime('now'))
        ",
            ("@fileName", request.FileName),
            ("@fileId", (object?)request.FileId ?? DBNull.Value),
            ("@folder", request.FolderPath ?? "GoogleDrive:/GATE_Sensei_Notes/"),
            ("@fileType", Path.GetExtension(request.FileName)),
            ("@summary", summary),
            ("@notes", string.Join("\n", revisionPoints)),
            ("@qCount", generatedQ.Count)
        );

        return new DriveProcessResult(request.FileName, summary, revisionPoints, generatedQ);
    }

    public async Task<string> BackupDatabaseToDriveAsync()
    {
        string dbSource = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sensei.db");
        string backupDir = Path.Combine(_syncFolder, "Backups");
        if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

        string backupFile = Path.Combine(backupDir, $"sensei_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db");
        if (File.Exists(dbSource))
        {
            File.Copy(dbSource, backupFile, true);
        }

        return await Task.FromResult(backupFile);
    }

    public object GetDriveStatus()
    {
        bool configured = AreCredentialsConfigured(out _, out _, out _, out var folderId);

        string lastSync = _lastSyncTimestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
        if (string.IsNullOrEmpty(lastSync))
        {
            try
            {
                var rows = _db.Query("SELECT MAX(synced_at) as last_sync FROM drive_files");
                if (rows.Count > 0 && rows[0]["last_sync"] != null)
                {
                    lastSync = rows[0]["last_sync"]?.ToString() ?? "";
                }
            }
            catch
            {
                // Ignore DB query errors in status probe
            }
        }
        if (string.IsNullOrEmpty(lastSync))
        {
            lastSync = "Not synced yet";
        }

        return new
        {
            Connected = configured,
            TargetFolder = configured ? $"GoogleDrive:/{folderId}" : "GoogleDrive:/GATE_Sensei_Notes/",
            LocalSyncPath = _syncFolder,
            AutoIngestEnabled = configured,
            LastSync = lastSync,
            Configured = configured
        };
    }

    public async Task<DriveSyncResult> SyncFromGoogleDriveAsync()
    {
        if (!AreCredentialsConfigured(out var clientId, out var clientSecret, out var refreshToken, out var folderId))
        {
            return new DriveSyncResult(
                Success: false,
                Message: "Google Drive credentials not configured. Please ensure GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, GOOGLE_REFRESH_TOKEN, and GDRIVE_FOLDER_ID are set in environment or .env.",
                SyncedCount: 0,
                TotalFilesFound: 0,
                SyncedFiles: new List<string>(),
                Configured: false
            );
        }

        string accessToken;
        try
        {
            accessToken = await GetAccessTokenAsync(clientId, clientSecret, refreshToken);
        }
        catch (Exception ex)
        {
            return new DriveSyncResult(
                Success: false,
                Message: $"Google OAuth token retrieval failed: {ex.Message}",
                SyncedCount: 0,
                TotalFilesFound: 0,
                SyncedFiles: new List<string>(),
                Configured: true
            );
        }

        // List text/markdown files in folder
        string query = $"'{folderId.Replace("'", "\\'")}' in parents and trashed = false";
        string listUrl = $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields={Uri.EscapeDataString("files(id, name, mimeType, modifiedTime, size)")}&pageSize=100";

        string listJson;
        try
        {
            using var listReq = new HttpRequestMessage(HttpMethod.Get, listUrl);
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var listRes = await _httpClient.SendAsync(listReq);
            listJson = await listRes.Content.ReadAsStringAsync();

            if (!listRes.IsSuccessStatusCode)
            {
                return new DriveSyncResult(
                    Success: false,
                    Message: $"Failed to list files from Google Drive (status {listRes.StatusCode}): {listJson}",
                    SyncedCount: 0,
                    TotalFilesFound: 0,
                    SyncedFiles: new List<string>(),
                    Configured: true
                );
            }
        }
        catch (Exception ex)
        {
            return new DriveSyncResult(
                Success: false,
                Message: $"Error querying Google Drive API: {ex.Message}",
                SyncedCount: 0,
                TotalFilesFound: 0,
                SyncedFiles: new List<string>(),
                Configured: true
            );
        }

        var matchingFiles = new List<(string Id, string Name, string MimeType)>();
        try
        {
            using var doc = JsonDocument.Parse(listJson);
            if (doc.RootElement.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filesProp.EnumerateArray())
                {
                    string id = f.TryGetProperty("id", out var idElem) ? idElem.GetString() ?? "" : "";
                    string name = f.TryGetProperty("name", out var nameElem) ? nameElem.GetString() ?? "" : "";
                    string mime = f.TryGetProperty("mimeType", out var mimeElem) ? mimeElem.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;
                    if (mime == "application/vnd.google-apps.folder") continue;

                    if (IsTextOrMarkdownFile(name, mime))
                    {
                        matchingFiles.Add((id, name, mime));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new DriveSyncResult(
                Success: false,
                Message: $"Failed to parse Google Drive API response: {ex.Message}",
                SyncedCount: 0,
                TotalFilesFound: 0,
                SyncedFiles: new List<string>(),
                Configured: true
            );
        }

        // Get currently ingested files to avoid duplicate processing
        var ingestedFiles = GetIngestedFiles();
        var ingestedIds = new HashSet<string>(
            ingestedFiles.Where(x => !string.IsNullOrEmpty(x.FileId)).Select(x => x.FileId!),
            StringComparer.OrdinalIgnoreCase
        );
        var ingestedNames = new HashSet<string>(
            ingestedFiles.Select(x => x.FileName),
            StringComparer.OrdinalIgnoreCase
        );

        var syncedFiles = new List<string>();
        var errors = new List<string>();

        foreach (var file in matchingFiles)
        {
            if (ingestedIds.Contains(file.Id) || ingestedNames.Contains(file.Name))
            {
                continue; // Already ingested
            }

            try
            {
                string content = await DownloadDriveFileContentAsync(file.Id, file.MimeType, accessToken);
                string targetFileName = file.Name;
                if (file.MimeType == "application/vnd.google-apps.document" && !Path.HasExtension(targetFileName))
                {
                    targetFileName += ".txt";
                }

                var req = new ProcessDriveFileRequest(
                    FileName: targetFileName,
                    Content: content,
                    FolderPath: $"GoogleDrive:/{folderId}",
                    FileId: file.Id
                );

                await IngestDriveFileAsync(req);
                syncedFiles.Add(targetFileName);
            }
            catch (Exception ex)
            {
                errors.Add($"{file.Name}: {ex.Message}");
            }
        }

        _lastSyncTimestamp = DateTime.UtcNow;

        string message = syncedFiles.Count > 0
            ? $"Successfully synced {syncedFiles.Count} new note(s) from Google Drive folder."
            : $"Google Drive folder checked. All {matchingFiles.Count} note(s) are already up to date.";

        if (errors.Count > 0)
        {
            message += $" Encountered {errors.Count} warning(s): {string.Join("; ", errors)}";
        }

        return new DriveSyncResult(
            Success: errors.Count == 0 || syncedFiles.Count > 0,
            Message: message,
            SyncedCount: syncedFiles.Count,
            TotalFilesFound: matchingFiles.Count,
            SyncedFiles: syncedFiles,
            Configured: true
        );
    }

    private async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedAccessToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "refresh_token", refreshToken },
            { "grant_type", "refresh_token" }
        };
        request.Content = new FormUrlEncodedContent(parameters);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google OAuth token request failed ({response.StatusCode}): {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
        {
            var token = tokenProp.GetString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                int expiresIn = 3600;
                if (doc.RootElement.TryGetProperty("expires_in", out var expProp) && expProp.TryGetInt32(out var exp))
                {
                    expiresIn = exp;
                }
                _cachedAccessToken = token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));
                return token;
            }
        }

        throw new InvalidOperationException("access_token missing in Google OAuth response: " + responseBody);
    }

    private async Task<string> DownloadDriveFileContentAsync(string fileId, string mimeType, string accessToken)
    {
        string url;
        if (mimeType == "application/vnd.google-apps.document")
        {
            url = $"https://www.googleapis.com/drive/v3/files/{fileId}/export?mimeType=text/plain";
        }
        else
        {
            url = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to download file '{fileId}' ({response.StatusCode}): {err}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static bool IsTextOrMarkdownFile(string name, string mimeType)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (ext is ".md" or ".markdown" or ".txt" or ".json" or ".csv" or ".org")
            return true;

        if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (mimeType == "application/vnd.google-apps.document")
            return true;

        return false;
    }

    private bool AreCredentialsConfigured(out string clientId, out string clientSecret, out string refreshToken, out string folderId)
    {
        clientId = GetConfigValue("GOOGLE_CLIENT_ID", "GoogleDrive:ClientId") ?? "";
        clientSecret = GetConfigValue("GOOGLE_CLIENT_SECRET", "GoogleDrive:ClientSecret") ?? "";
        refreshToken = GetConfigValue("GOOGLE_REFRESH_TOKEN", "GoogleDrive:RefreshToken") ?? "";
        folderId = SanitizeFolderId(GetConfigValue("GDRIVE_FOLDER_ID", "GOOGLE_DRIVE_FOLDER_ID") ?? "");

        return !string.IsNullOrWhiteSpace(clientId) &&
               !string.IsNullOrWhiteSpace(clientSecret) &&
               !string.IsNullOrWhiteSpace(refreshToken) &&
               !string.IsNullOrWhiteSpace(folderId);
    }

    private string? GetConfigValue(string key, string? altKey = null)
    {
        var val = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(val)) return val.Trim();

        if (altKey != null)
        {
            val = Environment.GetEnvironmentVariable(altKey);
            if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
        }

        if (_config != null)
        {
            val = _config[key];
            if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            if (altKey != null)
            {
                val = _config[altKey];
                if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            }
        }

        val = ReadFromDotEnv(key) ?? (altKey != null ? ReadFromDotEnv(altKey) : null);
        return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
    }

    private static string? ReadFromDotEnv(string key)
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                        var parts = trimmed.Split('=', 2);
                        if (parts[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            var val = parts[1].Trim();
                            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                            {
                                val = val.Substring(1, val.Length - 2);
                            }
                            return val;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore file read exceptions
        }
        return null;
    }

    private static string SanitizeFolderId(string folderId)
    {
        folderId = folderId.Trim();
        if (folderId.Contains("/folders/"))
        {
            var parts = folderId.Split(new[] { "/folders/" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var idPart = parts[1].Split('?')[0].Split('/')[0];
                if (!string.IsNullOrWhiteSpace(idPart)) return idPart;
            }
        }
        return folderId;
    }
}
