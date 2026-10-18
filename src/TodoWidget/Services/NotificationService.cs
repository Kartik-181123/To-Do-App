namespace TodoWidget.Services;

using System;
using System.Diagnostics;
using System.Linq;
using LiteDB;
using Microsoft.Toolkit.Uwp.Notifications;
using TodoWidget.Data;
using TodoWidget.Models;
public class NotificationService
{
    private static readonly Lazy<NotificationService> LazyInstance = new(() => new NotificationService());
    public static NotificationService Instance => LazyInstance.Value;
    public static event Action? StaticTasksChanged;
    public event Action? TasksChanged;

    private readonly AppDatabase _database;
    private readonly SettingsManager _settingsManager;
    private readonly object _initLock = new();
    private bool _isInitialized;
    public NotificationService(AppDatabase? database = null, SettingsManager? settingsManager = null)
    {
        _database = database ?? AppDatabase.Instance;
        _settingsManager = settingsManager ?? SettingsManager.Instance;
    }
    public void Initialize()
    {
        lock (_initLock)
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                ToastNotificationManagerCompat.OnActivated += HandleToastActivation;
                _isInitialized = true;
                Debug.WriteLine("[NotificationService] Initialized and subscribed to ToastNotificationManagerCompat.OnActivated.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationService] Failed to initialize toast activation handler: {ex.Message}");
            }
        }
    }
    public void SendTaskReminder(TodoTask task, int minutesBefore)
    {
        if (task == null)
        {
            return;
        }

        var settings = _settingsManager.Current;
        if (!settings.NotificationsEnabled)
        {
            return;
        }

        try
        {
            string title = minutesBefore > 0
                ? $"Task Due in {minutesBefore} minutes"
                : "Task Due Now";

            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(task.Title);

            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                builder.AddText(task.Description);
            }
            if (task.CanSnooze)
            {
                builder.AddButton(new ToastButton("Snooze", $"snooze:{task.Id}")
                    .SetBackgroundActivation());
            }
            builder.AddButton(new ToastButton("Mark Complete", $"complete:{task.Id}")
                .SetBackgroundActivation());

            if (!settings.NotificationSoundEnabled)
            {
                builder.AddAudio(new ToastAudio { Silent = true });
            }

            builder.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Failed to send reminder toast for task {task.Id}: {ex.Message}");
        }
    }
    public void SendTaskOverdue(TodoTask task)
    {
        if (task == null)
        {
            return;
        }

        var settings = _settingsManager.Current;
        if (!settings.NotificationsEnabled)
        {
            return;
        }

        try
        {
            string title = "Task Overdue";
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(task.Title);

            if (task.EffectiveDueDate.HasValue)
            {
                builder.AddText($"Was due: {task.EffectiveDueDate.Value:g}");
            }
            else if (!string.IsNullOrWhiteSpace(task.Description))
            {
                builder.AddText(task.Description);
            }

            builder.AddButton(new ToastButton("Mark Complete", $"complete:{task.Id}")
                .SetBackgroundActivation());

            if (!settings.NotificationSoundEnabled)
            {
                builder.AddAudio(new ToastAudio { Silent = true });
            }

            builder.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Failed to send overdue toast for task {task.Id}: {ex.Message}");
        }
    }
    public void SendStreakUpdate(int streak)
    {
        var settings = _settingsManager.Current;
        if (!settings.NotificationsEnabled)
        {
            return;
        }

        try
        {
            string title = "Streak Update!";
            string body = streak == 1
                ? "You completed all tasks for the day and started a 1-day streak!"
                : $"Awesome work! You've maintained your todo streak for {streak} consecutive days!";

            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(body);

            if (!settings.NotificationSoundEnabled)
            {
                builder.AddAudio(new ToastAudio { Silent = true });
            }

            builder.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Failed to send streak toast: {ex.Message}");
        }
    }
    private void HandleToastActivation(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        try
        {
            string rawArg = toastArgs.Argument ?? string.Empty;
            string action = rawArg;
            try
            {
                var parsedArgs = ToastArguments.Parse(rawArg);
                if (parsedArgs.TryGetValue("action", out var parsedAction))
                {
                    action = parsedAction;
                }
            }
            catch
            {
            }

            if (action.StartsWith("snooze:", StringComparison.OrdinalIgnoreCase))
            {
                string idStr = action["snooze:".Length..].Trim();
                HandleSnooze(idStr);
            }
            else if (action.StartsWith("complete:", StringComparison.OrdinalIgnoreCase))
            {
                string idStr = action["complete:".Length..].Trim();
                HandleComplete(idStr);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Error in HandleToastActivation: {ex.Message}");
        }
    }
    private void HandleSnooze(string idStr)
    {
        if (!TryParseObjectId(idStr, out var objectId))
        {
            Debug.WriteLine($"[NotificationService] Could not parse ObjectId '{idStr}' for snooze.");
            return;
        }

        var task = _database.GetAllTasks().FirstOrDefault(t => t.Id == objectId);
        if (task == null)
        {
            Debug.WriteLine($"[NotificationService] Task {objectId} not found for snooze.");
            return;
        }

        if (!task.CanSnooze)
        {
            Debug.WriteLine($"[NotificationService] Task {objectId} cannot be snoozed.");
            return;
        }

        int snoozeDuration = _settingsManager.Current.DefaultSnoozeDurationMinutes;
        if (snoozeDuration <= 0)
        {
            snoozeDuration = 15;
        }

        task.HasBeenSnoozed = true;
        task.SnoozedUntil = DateTime.Now.AddMinutes(snoozeDuration);

        _database.UpsertTask(task);
        Debug.WriteLine($"[NotificationService] Snoozed task '{task.Title}' until {task.SnoozedUntil:g}.");

        NotifyTasksChanged();
    }
    private void HandleComplete(string idStr)
    {
        if (!TryParseObjectId(idStr, out var objectId))
        {
            Debug.WriteLine($"[NotificationService] Could not parse ObjectId '{idStr}' for completion.");
            return;
        }

        var task = _database.GetAllTasks().FirstOrDefault(t => t.Id == objectId);
        if (task == null)
        {
            Debug.WriteLine($"[NotificationService] Task {objectId} not found for completion.");
            return;
        }

        task.Status = TodoTaskStatus.Completed;
        task.CompletedAt = DateTime.Now;

        _database.UpsertTask(task);
        Debug.WriteLine($"[NotificationService] Marked task '{task.Title}' as completed via notification action.");

        NotifyTasksChanged();
    }
    public static void NotifyTasksChanged()
    {
        try
        {
            if (LazyInstance.IsValueCreated)
            {
                LazyInstance.Value.TasksChanged?.Invoke();
            }
            StaticTasksChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Exception during TasksChanged dispatch: {ex.Message}");
        }
    }
    private static bool TryParseObjectId(string idStr, out ObjectId objectId)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(idStr))
            {
                objectId = new ObjectId(idStr);
                return true;
            }
        }
        catch
        {
        }

        objectId = ObjectId.Empty;
        return false;
    }
}
