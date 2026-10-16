namespace TodoWidget.Services;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TodoWidget.Data;
using TodoWidget.Models;
public sealed class StreakService : IDisposable
{
    private static readonly Lazy<StreakService> LazyInstance = new(() => new StreakService());
    public static StreakService Instance => LazyInstance.Value;
    public event Action<StreakData>? StreakChanged;
    public static event Action<StreakData>? StaticStreakChanged;

    private readonly AppDatabase _database;
    private readonly SettingsManager _settingsManager;
    private readonly NotificationService _notificationService;

    private readonly object _lock = new();
    private Timer? _midnightTimer;
    private bool _isDisposed;
    public StreakService(
        AppDatabase? database = null,
        SettingsManager? settingsManager = null,
        NotificationService? notificationService = null)
    {
        _database = database ?? AppDatabase.Instance;
        _settingsManager = settingsManager ?? SettingsManager.Instance;
        _notificationService = notificationService ?? NotificationService.Instance;

        ScheduleMidnightTimer();
    }
    public StreakData GetCurrentStreak()
    {
        lock (_lock)
        {
            return _database.GetStreakData() ?? new StreakData { Id = 1 };
        }
    }
    public StreakData EvaluateDay(DateTime date)
    {
        lock (_lock)
        {
            DateTime targetDate = date.Date;
            var tasks = _database.GetTasksDueOnDate(targetDate) ?? [];
            var streakData = _database.GetStreakData() ?? new StreakData { Id = 1 };
            if (tasks.Count == 0)
            {
                streakData.LastStreakDate = targetDate;
                _database.SaveStreakData(streakData);
                NotifyStreakChanged(streakData);
                Debug.WriteLine($"[StreakService] No tasks due on {targetDate:d}; streak count maintained at {streakData.CurrentStreak}.");
                return streakData;
            }
            bool allCompleted = tasks.All(t => t.Status == TodoTaskStatus.Completed);

            if (allCompleted)
            {
                streakData.CurrentStreak++;
                if (streakData.CurrentStreak > streakData.LongestStreak)
                {
                    streakData.LongestStreak = streakData.CurrentStreak;
                }

                streakData.TotalTasksCompleted += tasks.Count;
                _notificationService.SendStreakUpdate(streakData.CurrentStreak);
                Debug.WriteLine($"[StreakService] All tasks completed for {targetDate:d}! Streak is now {streakData.CurrentStreak}.");
            }
            else
            {
                int newlyFailed = 0;
                foreach (var task in tasks)
                {
                    if (task.Status == TodoTaskStatus.Pending)
                    {
                        task.Status = TodoTaskStatus.Failed;
                        _database.UpsertTask(task);
                        newlyFailed++;
                    }
                }

                streakData.CurrentStreak = 0;
                streakData.TotalTasksFailed += newlyFailed;
                Debug.WriteLine($"[StreakService] Not all tasks completed on {targetDate:d}. Reset streak to 0, {newlyFailed} tasks marked Failed.");
            }
            streakData.LastStreakDate = targetDate;
            _database.SaveStreakData(streakData);
            NotifyStreakChanged(streakData);

            return streakData;
        }
    }
    public void CheckAndEvaluateYesterday()
    {
        lock (_lock)
        {
            var streakData = GetCurrentStreak();
            DateTime yesterday = DateTime.Today.AddDays(-1);
            if (streakData.LastStreakDate == default || streakData.LastStreakDate == DateTime.MinValue)
            {
                EvaluateDay(yesterday);
                return;
            }
            if (streakData.LastStreakDate.Date < yesterday)
            {
                DateTime dayToEvaluate = streakData.LastStreakDate.Date.AddDays(1);
                while (dayToEvaluate <= yesterday)
                {
                    EvaluateDay(dayToEvaluate);
                    dayToEvaluate = dayToEvaluate.AddDays(1);
                }
            }
        }
    }
    private void ScheduleMidnightTimer()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _midnightTimer?.Dispose();

            DateTime now = DateTime.Now;
            DateTime nextMidnight = DateTime.Today.AddDays(1).AddSeconds(2);
            TimeSpan delay = nextMidnight - now;
            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromSeconds(5);
            }

            _midnightTimer = new Timer(_ =>
            {
                try
                {
                    Debug.WriteLine("[StreakService] Midnight timer fired, checking yesterday's streak.");
                    CheckAndEvaluateYesterday();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[StreakService] Midnight evaluation error: {ex.Message}");
                }
                finally
                {
                    ScheduleMidnightTimer();
                }
            }, null, delay, Timeout.InfiniteTimeSpan);
        }
    }
    private void NotifyStreakChanged(StreakData streakData)
    {
        try
        {
            StreakChanged?.Invoke(streakData);
            StaticStreakChanged?.Invoke(streakData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StreakService] Exception dispatching StreakChanged event: {ex.Message}");
        }
    }
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _midnightTimer?.Dispose();
            _midnightTimer = null;
            _isDisposed = true;
            Debug.WriteLine("[StreakService] Disposed.");
        }
    }
}
