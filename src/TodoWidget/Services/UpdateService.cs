namespace TodoWidget.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TodoWidget.Helpers;

public class UpdateInfo
{
    public string TagName { get; set; } = string.Empty;
    public Version Version { get; set; } = new(0, 0);
    public string DisplayVersion { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool IsPreRelease { get; set; }
}

public class UpdateService : ObservableObject
{
    private static readonly Lazy<UpdateService> _lazy = new(() => new UpdateService());
    public static UpdateService Instance => _lazy.Value;

    private const string GitHubApiReleasesUrl = "https://api.github.com/repos/Kartik-181123/To-Do-App/releases";
    private readonly HttpClient _httpClient;

    private bool _isChecking;
    private bool _isDownloading;
    private double _downloadProgress;
    private string _statusMessage = string.Empty;
    private UpdateInfo? _availableUpdate;

    public static Version CurrentVersion
    {
        get
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver != null ? new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build)) : new Version(4, 2, 0);
        }
    }

    public static string CurrentVersionDisplay => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build} (UI fixes and bugs)";

    public bool IsChecking
    {
        get => _isChecking;
        private set => SetProperty(ref _isChecking, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set => SetProperty(ref _isDownloading, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public UpdateInfo? AvailableUpdate
    {
        get => _availableUpdate;
        private set
        {
            if (SetProperty(ref _availableUpdate, value))
            {
                OnPropertyChanged(nameof(HasUpdateAvailable));
            }
        }
    }

    public bool HasUpdateAvailable => AvailableUpdate != null;

    private UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "To-Do-App-Updater");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool manualCheck = false)
    {
        if (IsChecking || IsDownloading) return null;

        IsChecking = true;
        StatusMessage = "Checking for updates...";

        try
        {
            using var response = await _httpClient.GetAsync(GitHubApiReleasesUrl).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                StatusMessage = manualCheck ? "Unable to reach GitHub update server." : string.Empty;
                return null;
            }

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                StatusMessage = manualCheck ? "No releases found." : string.Empty;
                return null;
            }

            UpdateInfo? highestUpdate = null;

            foreach (var release in doc.RootElement.EnumerateArray())
            {
                if (!release.TryGetProperty("tag_name", out var tagElem)) continue;
                string rawTag = tagElem.GetString() ?? string.Empty;

                var match = Regex.Match(rawTag, @"(\d+)\.(\d+)(?:\.(\d+))?");
                if (!match.Success) continue;

                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                var releaseVersion = new Version(major, minor, build);

                if (releaseVersion <= CurrentVersion) continue;

                if (!release.TryGetProperty("assets", out var assetsElem) || assetsElem.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                string downloadUrl = string.Empty;
                long assetSize = 0;

                foreach (var asset in assetsElem.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameElem) &&
                        asset.TryGetProperty("browser_download_url", out var urlElem))
                    {
                        string name = nameElem.GetString() ?? string.Empty;
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = urlElem.GetString() ?? string.Empty;
                            if (asset.TryGetProperty("size", out var sizeElem))
                            {
                                assetSize = sizeElem.GetInt64();
                            }
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl)) continue;

                bool isPre = release.TryGetProperty("prerelease", out var preElem) && preElem.GetBoolean();
                string title = release.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? rawTag : rawTag;
                string body = release.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;

                if (highestUpdate == null || releaseVersion > highestUpdate.Version)
                {
                    highestUpdate = new UpdateInfo
                    {
                        TagName = rawTag,
                        Version = releaseVersion,
                        DisplayVersion = $"{releaseVersion.Major}.{releaseVersion.Minor}.{releaseVersion.Build}{(isPre ? " Beta" : "")}",
                        Title = title,
                        ReleaseNotes = body,
                        DownloadUrl = downloadUrl,
                        FileSizeBytes = assetSize,
                        IsPreRelease = isPre
                    };
                }
            }

            AvailableUpdate = highestUpdate;

            if (highestUpdate != null)
            {
                StatusMessage = $"Update available: {highestUpdate.DisplayVersion}";
            }
            else
            {
                StatusMessage = manualCheck ? "You are using the latest version." : string.Empty;
            }

            return highestUpdate;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Error checking for updates: {ex.Message}");
            StatusMessage = manualCheck ? "Check failed. Please check your internet connection." : string.Empty;
            return null;
        }
        finally
        {
            IsChecking = false;
        }
    }

    public async Task<bool> DownloadAndInstallAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        if (update == null || string.IsNullOrWhiteSpace(update.DownloadUrl)) return false;
        if (IsDownloading) return false;

        IsDownloading = true;
        DownloadProgress = 0;
        StatusMessage = "Downloading update...";

        string tempDir = Path.GetTempPath();
        string installerPath = Path.Combine(tempDir, $"ToDoSetup-{update.TagName}.exe");

        try
        {
            using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength ?? (update.FileSizeBytes > 0 ? update.FileSizeBytes : null);

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    double progress = Math.Round((double)totalRead / totalBytes.Value * 100.0, 1);
                    DownloadProgress = progress;
                    StatusMessage = $"Downloading: {progress:F0}%";
                }
            }

            fileStream.Close();

            StatusMessage = "Installing update silently & restarting...";

            string currentExe = Environment.ProcessPath ?? string.Empty;
            string restartScript = Path.Combine(tempDir, "todo_silent_updater.bat");

            string scriptContent = $@"@echo off
start /wait """" ""{installerPath}"" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS
timeout /t 1 > nul
start """" ""{currentExe}""
del ""%~f0""
";
            File.WriteAllText(restartScript, scriptContent);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{restartScript}\"\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Error downloading update: {ex.Message}");
            StatusMessage = "Download failed. Please try again later.";
            try
            {
                if (File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                }
            }
            catch
            {
            }
            return false;
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
