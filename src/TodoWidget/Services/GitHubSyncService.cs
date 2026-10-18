namespace TodoWidget.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;

public class SyncPayload
{
    public string AppVersion { get; set; } = "4.0.0";
    public DateTime SyncedAtUtc { get; set; } = DateTime.UtcNow;
    public StreakData? Streak { get; set; }
    public List<TodoTask> Tasks { get; set; } = new();
}

public class GitHubSyncService : ObservableObject
{
    private static readonly Lazy<GitHubSyncService> _lazy = new(() => new GitHubSyncService());
    public static GitHubSyncService Instance => _lazy.Value;

    public static event Action? StaticSyncCompleted;

    private const string GistFilename = "todo-app-sync-data.json";
    private readonly HttpClient _httpClient;

    private bool _isSyncing;
    private string _statusMessage = string.Empty;
    private string _username = string.Empty;
    private string _avatarUrl = string.Empty;
    private DateTime? _lastSyncTime;

    public bool IsSyncing
    {
        get => _isSyncing;
        private set => SetProperty(ref _isSyncing, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string Username
    {
        get => _username;
        private set => SetProperty(ref _username, value);
    }

    public string AvatarUrl
    {
        get => _avatarUrl;
        private set => SetProperty(ref _avatarUrl, value);
    }

    public DateTime? LastSyncTime
    {
        get => _lastSyncTime;
        private set
        {
            if (SetProperty(ref _lastSyncTime, value))
            {
                OnPropertyChanged(nameof(LastSyncText));
            }
        }
    }

    public string LastSyncText
    {
        get
        {
            if (!_lastSyncTime.HasValue) return "Never synced";
            var localTime = _lastSyncTime.Value.ToLocalTime();
            var span = DateTime.Now - localTime;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"Today at {localTime:h:mm tt}";
            return localTime.ToString("MMM d, yyyy h:mm tt");
        }
    }

    public bool IsConnected => !string.IsNullOrWhiteSpace(SettingsManager.Instance.Current.GitHubToken);

    private GitHubSyncService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "To-Do-Windows-App");
        _httpClient.Timeout = TimeSpan.FromSeconds(25);

        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var settings = SettingsManager.Instance.Current;
        Username = settings.GitHubUsername;
        AvatarUrl = settings.GitHubAvatarUrl;
        LastSyncTime = settings.LastSyncUtc;
        OnPropertyChanged(nameof(IsConnected));
    }

    public async Task<bool> ConnectAccountAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            StatusMessage = "Token cannot be empty.";
            return false;
        }

        token = token.Trim();
        IsSyncing = true;
        StatusMessage = "Authenticating with GitHub...";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                StatusMessage = "Invalid GitHub token. Please verify token permissions.";
                return false;
            }

            string userJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var userDoc = JsonDocument.Parse(userJson);

            string login = userDoc.RootElement.GetProperty("login").GetString() ?? "User";
            string avatar = userDoc.RootElement.TryGetProperty("avatar_url", out var av) ? av.GetString() ?? "" : "";

            var settings = SettingsManager.Instance.Current;
            settings.GitHubToken = token;
            settings.GitHubUsername = login;
            settings.GitHubAvatarUrl = avatar;
            SettingsManager.Instance.Save();

            Username = login;
            AvatarUrl = avatar;
            OnPropertyChanged(nameof(IsConnected));

            StatusMessage = $"Connected as @{login}!";

            // Locate or create Gist
            await EnsureGistExistsAsync(token).ConfigureAwait(false);

            // Initial Sync
            await SyncNowAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GitHubSync] Auth error: {ex.Message}");
            StatusMessage = "Connection failed. Please check your network.";
            return false;
        }
        finally
        {
            IsSyncing = false;
        }
    }

    public void DisconnectAccount()
    {
        var settings = SettingsManager.Instance.Current;
        settings.GitHubToken = string.Empty;
        settings.GitHubUsername = string.Empty;
        settings.GitHubAvatarUrl = string.Empty;
        settings.GitHubGistId = string.Empty;
        settings.LastSyncUtc = null;
        SettingsManager.Instance.Save();

        Username = string.Empty;
        AvatarUrl = string.Empty;
        LastSyncTime = null;
        StatusMessage = "Disconnected from GitHub.";
        OnPropertyChanged(nameof(IsConnected));
    }

    public async Task<bool> SyncNowAsync()
    {
        var settings = SettingsManager.Instance.Current;
        if (string.IsNullOrWhiteSpace(settings.GitHubToken))
        {
            StatusMessage = "No GitHub account connected.";
            return false;
        }

        IsSyncing = true;
        StatusMessage = "Syncing with cloud...";

        try
        {
            string token = settings.GitHubToken;
            string gistId = await EnsureGistExistsAsync(token).ConfigureAwait(false);
            if (string.IsNullOrEmpty(gistId))
            {
                StatusMessage = "Failed to locate sync Gist.";
                return false;
            }

            // 1. Fetch remote Gist data
            using var getReq = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/gists/{gistId}");
            getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            getReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var getResp = await _httpClient.SendAsync(getReq).ConfigureAwait(false);
            if (!getResp.IsSuccessStatusCode)
            {
                StatusMessage = "Failed to read remote cloud data.";
                return false;
            }

            string gistJson = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var gistDoc = JsonDocument.Parse(gistJson);

            SyncPayload? remotePayload = null;
            if (gistDoc.RootElement.TryGetProperty("files", out var filesElem) &&
                filesElem.TryGetProperty(GistFilename, out var targetFile) &&
                targetFile.TryGetProperty("content", out var contentElem))
            {
                string rawContent = contentElem.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(rawContent))
                {
                    try
                    {
                        remotePayload = JsonSerializer.Deserialize<SyncPayload>(rawContent);
                    }
                    catch
                    {
                        // Ignore corrupt json and recreate
                    }
                }
            }

            // 2. Perform two-way merge
            var database = AppDatabase.Instance;
            var localTasks = database.GetAllTasks();
            var localStreak = database.GetStreakData();

            var mergedTasksMap = new Dictionary<string, TodoTask>();

            // Add all local tasks
            foreach (var t in localTasks)
            {
                mergedTasksMap[t.Id.ToString()] = t;
            }

            // Merge remote tasks
            if (remotePayload?.Tasks != null)
            {
                foreach (var rTask in remotePayload.Tasks)
                {
                    string key = rTask.Id.ToString();
                    if (mergedTasksMap.TryGetValue(key, out var localMatch))
                    {
                        // Priority to completed state or newer update time
                        if (rTask.Status == TodoTaskStatus.Completed && localMatch.Status != TodoTaskStatus.Completed)
                        {
                            localMatch.Status = TodoTaskStatus.Completed;
                            localMatch.CompletedAt = rTask.CompletedAt ?? DateTime.Now;
                            database.UpsertTask(localMatch);
                        }
                        else if (rTask.UpdatedAt > localMatch.UpdatedAt)
                        {
                            localMatch.Title = rTask.Title;
                            localMatch.Description = rTask.Description;
                            localMatch.DueDate = rTask.DueDate;
                            localMatch.Priority = rTask.Priority;
                            localMatch.Status = rTask.Status;
                            localMatch.UpdatedAt = rTask.UpdatedAt;
                            database.UpsertTask(localMatch);
                        }
                    }
                    else
                    {
                        // New task from remote
                        database.UpsertTask(rTask);
                        mergedTasksMap[key] = rTask;
                    }
                }
            }

            // Merge streak
            if (remotePayload?.Streak != null)
            {
                bool streakModified = false;
                if (remotePayload.Streak.CurrentStreak > localStreak.CurrentStreak)
                {
                    localStreak.CurrentStreak = remotePayload.Streak.CurrentStreak;
                    streakModified = true;
                }
                if (remotePayload.Streak.LongestStreak > localStreak.LongestStreak)
                {
                    localStreak.LongestStreak = remotePayload.Streak.LongestStreak;
                    streakModified = true;
                }
                if (remotePayload.Streak.TotalTasksCompleted > localStreak.TotalTasksCompleted)
                {
                    localStreak.TotalTasksCompleted = remotePayload.Streak.TotalTasksCompleted;
                    streakModified = true;
                }
                if (remotePayload.Streak.LastStreakDate > localStreak.LastStreakDate)
                {
                    localStreak.LastStreakDate = remotePayload.Streak.LastStreakDate;
                    streakModified = true;
                }

                if (streakModified)
                {
                    database.SaveStreakData(localStreak);
                }
            }

            // 3. Serialize and push merged payload back to GitHub Gist
            var updatedTasks = database.GetAllTasks();
            var updatedStreak = database.GetStreakData();

            var newPayload = new SyncPayload
            {
                AppVersion = "4.0.0",
                SyncedAtUtc = DateTime.UtcNow,
                Streak = updatedStreak,
                Tasks = updatedTasks
            };

            string serialized = JsonSerializer.Serialize(newPayload, new JsonSerializerOptions { WriteIndented = false });

            var patchBody = new
            {
                description = "To-Do App Cloud Sync Data",
                files = new Dictionary<string, object>
                {
                    { GistFilename, new { content = serialized } }
                }
            };

            using var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"https://api.github.com/gists/{gistId}");
            patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            patchReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            patchReq.Content = new StringContent(JsonSerializer.Serialize(patchBody), Encoding.UTF8, "application/json");

            using var patchResp = await _httpClient.SendAsync(patchReq).ConfigureAwait(false);
            if (patchResp.IsSuccessStatusCode)
            {
                var now = DateTime.UtcNow;
                settings.LastSyncUtc = now;
                SettingsManager.Instance.Save();
                LastSyncTime = now;
                StatusMessage = "Sync completed successfully!";

                StaticSyncCompleted?.Invoke();
                return true;
            }
            else
            {
                StatusMessage = "Failed to update cloud Gist.";
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GitHubSync] Sync error: {ex.Message}");
            StatusMessage = "Sync error: check internet connection.";
            return false;
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task<string> EnsureGistExistsAsync(string token)
    {
        var settings = SettingsManager.Instance.Current;
        if (!string.IsNullOrEmpty(settings.GitHubGistId))
        {
            return settings.GitHubGistId;
        }

        try
        {
            // List user gists
            using var listReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/gists?per_page=100");
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            listReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var listResp = await _httpClient.SendAsync(listReq).ConfigureAwait(false);
            if (listResp.IsSuccessStatusCode)
            {
                string listJson = await listResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var listDoc = JsonDocument.Parse(listJson);

                if (listDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in listDoc.RootElement.EnumerateArray())
                    {
                        if (g.TryGetProperty("files", out var filesObj) &&
                            filesObj.TryGetProperty(GistFilename, out _))
                        {
                            string foundId = g.GetProperty("id").GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(foundId))
                            {
                                settings.GitHubGistId = foundId;
                                SettingsManager.Instance.Save();
                                return foundId;
                            }
                        }
                    }
                }
            }

            // Not found, create private Gist
            var initialPayload = new SyncPayload
            {
                AppVersion = "4.0.0",
                SyncedAtUtc = DateTime.UtcNow,
                Streak = AppDatabase.Instance.GetStreakData(),
                Tasks = AppDatabase.Instance.GetAllTasks()
            };

            var postBody = new
            {
                description = "To-Do App Cloud Sync Data",
                @public = false,
                files = new Dictionary<string, object>
                {
                    { GistFilename, new { content = JsonSerializer.Serialize(initialPayload) } }
                }
            };

            using var postReq = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/gists");
            postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            postReq.Content = new StringContent(JsonSerializer.Serialize(postBody), Encoding.UTF8, "application/json");

            using var postResp = await _httpClient.SendAsync(postReq).ConfigureAwait(false);
            if (postResp.IsSuccessStatusCode)
            {
                string postJson = await postResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var postDoc = JsonDocument.Parse(postJson);
                string newId = postDoc.RootElement.GetProperty("id").GetString() ?? string.Empty;

                settings.GitHubGistId = newId;
                SettingsManager.Instance.Save();
                return newId;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GitHubSync] EnsureGistExists error: {ex.Message}");
        }

        return string.Empty;
    }
}
