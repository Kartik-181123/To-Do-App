namespace TodoWidget.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;
using TodoWidget.Services;

public class WidgetViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly NotificationService? _notificationService;
    private readonly StreakService? _streakService;
    private readonly DispatcherTimer _timer;

    private ObservableCollection<TodoTask> _tasks = new();
    private int _currentStreak;
    private string _streakText = "0 Days";
    private string _newTaskTitle = string.Empty;
    private DateTime? _newTaskDueDate;
    private TaskPriority _newTaskPriority = TaskPriority.Medium;
    private bool _hasTasks;

    // Focus Mode & Timer state
    private bool _isFocusMode;
    private TodoTask? _focusTask;
    private int _timerRemainingSeconds = 120; // Default 2-min kickstart
    private int _timerTotalSeconds = 120;
    private bool _isTimerRunning;
    private string _timerMode = "Sprint"; // "Sprint" (2m) or "Pomodoro" (25m)

    // Daily Progress Bar
    private int _dailyCompletedCount;
    private int _dailyTotalCount;
    private double _dailyProgressPercentage;
    private string _dailyProgressText = "0 / 0 (0%)";
    private bool _isDailyComplete;

    public event Action? RequestOpenDashboard;

    public ObservableCollection<TodoTask> Tasks
    {
        get => _tasks;
        set
        {
            if (SetProperty(ref _tasks, value))
            {
                HasTasks = _tasks.Count > 0;
                UpdateFocusTask();
                UpdateDailyProgress();
            }
        }
    }

    public int CurrentStreak
    {
        get => _currentStreak;
        set
        {
            if (SetProperty(ref _currentStreak, value))
            {
                StreakText = value > 0
                    ? $"{value} {(value == 1 ? "Day" : "Days")}"
                    : "0 Days";
            }
        }
    }

    public string StreakText
    {
        get => _streakText;
        set => SetProperty(ref _streakText, value);
    }

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set => SetProperty(ref _newTaskTitle, value);
    }

    public DateTime? NewTaskDueDate
    {
        get => _newTaskDueDate;
        set => SetProperty(ref _newTaskDueDate, value);
    }

    public TaskPriority NewTaskPriority
    {
        get => _newTaskPriority;
        set => SetProperty(ref _newTaskPriority, value);
    }

    public bool HasTasks
    {
        get => _hasTasks;
        private set => SetProperty(ref _hasTasks, value);
    }

    // Focus Mode Properties
    public bool IsFocusMode
    {
        get => _isFocusMode;
        set
        {
            if (SetProperty(ref _isFocusMode, value))
            {
                var settings = SettingsManager.Instance.Current;
                settings.IsFocusModeActive = value;
                SettingsManager.Instance.Save();
                if (value)
                {
                    UpdateFocusTask();
                }
            }
        }
    }

    public TodoTask? FocusTask
    {
        get => _focusTask;
        private set => SetProperty(ref _focusTask, value);
    }

    public string TimerDisplay
    {
        get
        {
            int m = _timerRemainingSeconds / 60;
            int s = _timerRemainingSeconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }

    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        private set => SetProperty(ref _isTimerRunning, value);
    }

    public string TimerMode
    {
        get => _timerMode;
        set
        {
            if (SetProperty(ref _timerMode, value))
            {
                ResetTimer();
            }
        }
    }

    // Daily Progress Properties
    public int DailyCompletedCount
    {
        get => _dailyCompletedCount;
        private set => SetProperty(ref _dailyCompletedCount, value);
    }

    public int DailyTotalCount
    {
        get => _dailyTotalCount;
        private set => SetProperty(ref _dailyTotalCount, value);
    }

    public double DailyProgressPercentage
    {
        get => _dailyProgressPercentage;
        private set => SetProperty(ref _dailyProgressPercentage, value);
    }

    public string DailyProgressText
    {
        get => _dailyProgressText;
        private set => SetProperty(ref _dailyProgressText, value);
    }

    public bool IsDailyComplete
    {
        get => _isDailyComplete;
        private set => SetProperty(ref _isDailyComplete, value);
    }

    // Commands
    public RelayCommand<TodoTask> CompleteTaskCommand { get; }
    public RelayCommand<TodoTask> DeleteTaskCommand { get; }
    public RelayCommand<TodoTask> ToggleVoiceNoteCommand { get; }
    public RelayCommand OpenDashboardCommand { get; }
    public RelayCommand AddQuickTaskCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public RelayCommand ToggleFocusModeCommand { get; }
    public RelayCommand ToggleTimerCommand { get; }
    public RelayCommand ResetTimerCommand { get; }
    public RelayCommand<string> SetTimerModeCommand { get; }
    public RelayCommand CompleteFocusTaskCommand { get; }

    public WidgetViewModel(
        AppDatabase database,
        NotificationService? notificationService = null,
        StreakService? streakService = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _notificationService = notificationService;
        _streakService = streakService;

        _isFocusMode = SettingsManager.Instance.Current.IsFocusModeActive;

        // Initialize Countdown Timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;

        CompleteTaskCommand = new RelayCommand<TodoTask>(ExecuteCompleteTask);
        DeleteTaskCommand = new RelayCommand<TodoTask>(ExecuteDeleteTask);
        ToggleVoiceNoteCommand = new RelayCommand<TodoTask>(task =>
        {
            if (task != null && task.HasVoiceNote)
            {
                AudioService.Instance.TogglePlay(task.VoiceNotePath);
            }
        });
        OpenDashboardCommand = new RelayCommand(ExecuteOpenDashboard);
        AddQuickTaskCommand = new RelayCommand(ExecuteAddQuickTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
        RefreshCommand = new RelayCommand(() =>
        {
            RefreshTasks();
            RefreshStreak();
        });

        ToggleFocusModeCommand = new RelayCommand(() => IsFocusMode = !IsFocusMode);
        ToggleTimerCommand = new RelayCommand(ExecuteToggleTimer);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        SetTimerModeCommand = new RelayCommand<string>(mode =>
        {
            if (!string.IsNullOrEmpty(mode))
            {
                TimerMode = mode;
            }
        });
        CompleteFocusTaskCommand = new RelayCommand(() =>
        {
            if (FocusTask != null)
            {
                ExecuteCompleteTask(FocusTask);
            }
        });

        if (_notificationService != null)
        {
            _notificationService.TasksChanged += OnBackgroundTasksChanged;
        }
        NotificationService.StaticTasksChanged += OnBackgroundTasksChanged;
        GitHubSyncService.StaticSyncCompleted += OnBackgroundTasksChanged;

        if (_streakService != null)
        {
            _streakService.StreakChanged += OnBackgroundStreakChanged;
        }

        ResetTimer();
        RefreshTasks();
        RefreshStreak();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_timerRemainingSeconds > 0)
        {
            _timerRemainingSeconds--;
            OnPropertyChanged(nameof(TimerDisplay));
        }
        else
        {
            _timer.Stop();
            IsTimerRunning = false;
            OnPropertyChanged(nameof(TimerDisplay));
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch { }
        }
    }

    private void ExecuteToggleTimer()
    {
        if (IsTimerRunning)
        {
            _timer.Stop();
            IsTimerRunning = false;
        }
        else
        {
            if (_timerRemainingSeconds <= 0)
            {
                ResetTimer();
            }
            _timer.Start();
            IsTimerRunning = true;
        }
    }

    private void ResetTimer()
    {
        _timer.Stop();
        IsTimerRunning = false;
        if (_timerMode == "Pomodoro")
        {
            _timerRemainingSeconds = SettingsManager.Instance.Current.FocusPomodoroMinutes * 60;
        }
        else
        {
            _timerRemainingSeconds = SettingsManager.Instance.Current.FocusSprintMinutes * 60;
        }
        _timerTotalSeconds = _timerRemainingSeconds;
        OnPropertyChanged(nameof(TimerDisplay));
    }

    public void RefreshTasks()
    {
        RunOnDispatcher(() =>
        {
            var pendingTasks = _database.GetPendingTasks() ?? new();
            var todayTasks = _database.GetTasksDueOnDate(DateTime.Today) ?? new();
            var combined = pendingTasks
                .Concat(todayTasks)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .OrderBy(t => t.Status == TodoTaskStatus.Completed ? 1 : 0) // Incomplete first
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.EffectiveDueDate ?? DateTime.MaxValue)
                .ToList();

            Tasks = new ObservableCollection<TodoTask>(combined);
        });
    }

    private void UpdateFocusTask()
    {
        FocusTask = Tasks.FirstOrDefault(t => t.Status == TodoTaskStatus.Pending);
    }

    private void UpdateDailyProgress()
    {
        var allToday = _database.GetTasksDueOnDate(DateTime.Today) ?? new();
        if (allToday.Count == 0)
        {
            // Fallback to all current tasks if none specifically due today
            allToday = Tasks.ToList();
        }

        DailyTotalCount = allToday.Count;
        DailyCompletedCount = allToday.Count(t => t.Status == TodoTaskStatus.Completed);

        if (DailyTotalCount > 0)
        {
            DailyProgressPercentage = Math.Round((double)DailyCompletedCount / DailyTotalCount * 100.0, 0);
            DailyProgressText = $"{DailyCompletedCount} / {DailyTotalCount} completed ({DailyProgressPercentage:F0}%)";
            IsDailyComplete = DailyCompletedCount == DailyTotalCount;
        }
        else
        {
            DailyProgressPercentage = 0;
            DailyProgressText = "No tasks scheduled today";
            IsDailyComplete = false;
        }
    }

    public void RefreshStreak()
    {
        RunOnDispatcher(() =>
        {
            if (_streakService != null)
            {
                var streakData = _streakService.GetCurrentStreak();
                CurrentStreak = streakData?.CurrentStreak ?? 0;
            }
            else
            {
                var streakData = _database.GetStreakData();
                CurrentStreak = streakData?.CurrentStreak ?? 0;
            }
        });
    }

    private void ExecuteCompleteTask(TodoTask? task)
    {
        if (task == null) return;

        if (task.Status == TodoTaskStatus.Completed)
        {
            task.Status = TodoTaskStatus.Pending;
            task.CompletedAt = null;
        }
        else
        {
            var result = MessageBox.Show(
                $"Mark this task as completed?\n\n\"{task.Title}\"",
                "Complete Task",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            task.Status = TodoTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
        }

        _database.UpsertTask(task);
        RefreshTasks();
        RefreshStreak();
        TriggerAutoSync();
    }

    private void ExecuteDeleteTask(TodoTask? task)
    {
        if (task == null) return;

        _database.DeleteTask(task.Id);
        RefreshTasks();
        RefreshStreak();
        TriggerAutoSync();
    }

    private void ExecuteOpenDashboard()
    {
        RequestOpenDashboard?.Invoke();
    }

    private void ExecuteAddQuickTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

        var task = new TodoTask
        {
            Title = NewTaskTitle.Trim(),
            DueDate = NewTaskDueDate ?? DateTime.Today.AddHours(23).AddMinutes(59),
            Priority = NewTaskPriority,
            Status = TodoTaskStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _database.UpsertTask(task);

        NewTaskTitle = string.Empty;
        NewTaskDueDate = null;
        NewTaskPriority = TaskPriority.Medium;

        RefreshTasks();
        TriggerAutoSync();
    }

    private void TriggerAutoSync()
    {
        if (SettingsManager.Instance.Current.AutoSyncEnabled && GitHubSyncService.Instance.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                await GitHubSyncService.Instance.SyncNowAsync().ConfigureAwait(false);
            });
        }
    }

    private void OnBackgroundTasksChanged()
    {
        RefreshTasks();
    }

    private void OnBackgroundStreakChanged(StreakData? data)
    {
        RefreshStreak();
    }

    private void RunOnDispatcher(Action action)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }
}
