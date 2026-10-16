namespace TodoWidget.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TodoWidget.Data;
using TodoWidget.Models;
public sealed class ReminderService : IDisposable
{
    public const int OverdueOffset = -1;

    private static readonly Lazy<ReminderService> LazyInstance = new(() => new ReminderService());
    public static ReminderService Instance => LazyInstance.Value;

    private readonly AppDatabase _database;
    private readonly SettingsManager _settingsManager;
    private readonly NotificationService _notificationService;

    private readonly object _timerLock = new();
    private Timer? _timer;
    private bool _isRunning;
    private bool _isDisposed;
    public ReminderService(
        AppDatabase? database = null,
        SettingsManager? settingsManager = null,
        NotificationService? notificationService = null)
    {
        _database = database ?? AppDatabase.Instance;
        _settingsManager = settingsManager ?? SettingsManager.Instance;
        _notificationService = notificationService ?? NotificationService.Instance;
    }
    public void Start()
    {
        lock (_timerLock)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ReminderService));
            }

            if (_isRunning)
            {
                return;
            }
            _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            _isRunning = true;
            Debug.WriteLine("[ReminderService] Started background timer (30-second interval).");
        }
    }
    public void Stop()
    {
        lock (_timerLock)
        {
            if (!_isRunning || _timer == null)
            {
                return;
            }

            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _isRunning = false;
            Debug.WriteLine("[ReminderService] Stopped background timer.");
        }
    }
    private void OnTimerTick(object? state)
    {
        if (!Monitor.TryEnter(_timerLock))
        {
            return;
        }

        try
        {
            CheckReminders();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderService] Error during reminder scan: {ex.Message}");
        }
        finally
        {
            Monitor.Exit(_timerLock);
        }
    }
    public void CheckReminders()
    {
        var settings = _settingsManager.Current;
        if (!settings.NotificationsEnabled)
        {
            return;
        }

        DateTime now = DateTime.Now;
        var pendingTasks = _database.GetPendingTasks()
            .Where(t => t.EffectiveDueDate.HasValue)
            .ToList();

        if (pendingTasks.Count == 0)
        {
            return;
        }
        var intervals = (settings.ReminderIntervalsMinutes ?? new List<int> { 60, 30, 15 })
            .Where(m => m > 0)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        foreach (var task in pendingTasks)
        {
            task.FiredReminderOffsets ??= new HashSet<int>();
            DateTime due = task.EffectiveDueDate!.Value;
            bool taskModified = false;
            if (due < now)
            {
                if (!task.FiredReminderOffsets.Contains(OverdueOffset))
                {
                    _notificationService.SendTaskOverdue(task);
                    task.FiredReminderOffsets.Add(OverdueOffset);
                    taskModified = true;
                    Debug.WriteLine($"[ReminderService] Task '{task.Title}' marked as overdue.");
                }
            }
            else
            {
                TimeSpan remaining = due - now;
                foreach (int interval in intervals)
                {
                    if (remaining <= TimeSpan.FromMinutes(interval))
                    {
                        if (!task.FiredReminderOffsets.Contains(interval))
                        {
                            _notificationService.SendTaskReminder(task, interval);
                            task.FiredReminderOffsets.Add(interval);
                            taskModified = true;
                            Debug.WriteLine($"[ReminderService] Fired {interval}-min reminder for task '{task.Title}'.");
                        }
                        break;
                    }
                }
            }

            if (taskModified)
            {
                _database.UpsertTask(task);
            }
        }
    }
    public void Dispose()
    {
        lock (_timerLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
            _isDisposed = true;
            Debug.WriteLine("[ReminderService] Disposed.");
        }
    }
}
