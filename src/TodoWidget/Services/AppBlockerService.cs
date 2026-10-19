namespace TodoWidget.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TodoWidget.Data;
using TodoWidget.Models;

/// <summary>
/// High-performance, non-destructive background process monitor that enforces Focus Mode.
/// Automatically terminates designated distraction applications and Task Manager (if anti-cheat enabled).
/// Cleanly restores complete system normality immediately upon exiting Focus Mode.
/// </summary>
public class AppBlockerService : IDisposable
{
    private static readonly Lazy<AppBlockerService> _lazy = new(() => new AppBlockerService());
    public static AppBlockerService Instance => _lazy.Value;

    private Timer? _monitorTimer;
    private readonly object _lock = new();
    private int _isEvaluating = 0;
    private int _blockedCountThisSession = 0;
    private readonly ConcurrentDictionary<string, DateTime> _lastNotificationTimes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ImmuneProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "todowidget", "explorer", "dwm", "svchost", "csrss", "smss", "wininit", "services",
        "lsass", "winlogon", "fontdrvhost", "sihost", "taskhostw", "ctfmon", "system", "idle"
    };

    public bool IsRunning { get; private set; }

    public int BlockedCountThisSession
    {
        get => _blockedCountThisSession;
        private set
        {
            _blockedCountThisSession = value;
            BlockedCountChanged?.Invoke();
        }
    }

    public event Action<string, string>? AppBlocked;
    public event Action? BlockedCountChanged;

    private AppBlockerService()
    {
    }

    /// <summary>
    /// Starts the background process monitor for active focus session.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            _lastNotificationTimes.Clear();

            // Run check every 400 milliseconds for prompt, responsive enforcement
            _monitorTimer = new Timer(CheckProcessesCallback, null, 100, 400);
            Debug.WriteLine("[AppBlockerService] Started focus process monitor.");
        }
    }

    /// <summary>
    /// Stops the background process monitor and frees resources.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _monitorTimer?.Dispose();
            _monitorTimer = null;
            Debug.WriteLine("[AppBlockerService] Stopped focus process monitor.");
        }
    }

    public void ResetSessionCount()
    {
        BlockedCountThisSession = 0;
    }

    private void CheckProcessesCallback(object? state)
    {
        if (!IsRunning)
            return;

        // Prevent overlapping evaluations
        if (Interlocked.CompareExchange(ref _isEvaluating, 1, 0) != 0)
            return;

        try
        {
            var settings = SettingsManager.Instance.Current;
            if (!settings.IsFocusModeActive)
            {
                Stop();
                return;
            }

            // Build target lookup set for this cycle
            var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (settings.AppBlockingEnabled && settings.BlockedApps != null)
            {
                foreach (var app in settings.BlockedApps)
                {
                    if (app != null && app.IsEnabled && !string.IsNullOrWhiteSpace(app.ProcessName))
                    {
                        string pName = CleanProcessName(app.ProcessName);
                        if (!string.IsNullOrEmpty(pName) && !ImmuneProcesses.Contains(pName))
                        {
                            targets[pName] = string.IsNullOrWhiteSpace(app.Name) ? pName : app.Name;
                        }
                    }
                }
            }

            // Anti-cheat Task Manager blocking during focus mode
            if (settings.BlockTaskManagerInFocus)
            {
                targets["taskmgr"] = "Task Manager";
            }

            if (targets.Count == 0)
                return;

            Process[] runningProcesses;
            try
            {
                runningProcesses = Process.GetProcesses();
            }
            catch
            {
                return;
            }

            foreach (var proc in runningProcesses)
            {
                try
                {
                    string pName = proc.ProcessName;
                    if (targets.TryGetValue(pName, out string? appDisplayName))
                    {
                        // Match found - terminate immediately
                        try
                        {
                            proc.Kill(entireProcessTree: true);
                            BlockedCountThisSession++;

                            Debug.WriteLine($"[AppBlockerService] Blocked and terminated: {pName} ({appDisplayName})");

                            NotifyBlocked(pName, appDisplayName);
                            AppBlocked?.Invoke(appDisplayName, pName);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AppBlockerService] Error terminating {pName}: {ex.Message}");
                        }
                    }
                }
                catch
                {
                    // Ignore process access or lifecycle errors
                }
                finally
                {
                    try
                    {
                        proc.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppBlockerService] Unexpected error during check: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _isEvaluating, 0);
        }
    }

    private void NotifyBlocked(string processName, string appDisplayName)
    {
        DateTime now = DateTime.UtcNow;
        if (_lastNotificationTimes.TryGetValue(processName, out var lastTime))
        {
            // Debounce notifications per process to prevent toast spam (15 second cooldown)
            if ((now - lastTime).TotalSeconds < 15)
                return;
        }

        _lastNotificationTimes[processName] = now;

        try
        {
            NotificationService.Instance.SendAppBlocked(appDisplayName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppBlockerService] Toast dispatch error: {ex.Message}");
        }
    }

    private static string CleanProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return string.Empty;

        string clean = processName.Trim();
        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[..^4];
        }
        return clean.Trim();
    }

    public void Dispose()
    {
        Stop();
    }
}
