namespace TodoWidget.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;
using TodoWidget.Services;
public class WidgetViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly NotificationService? _notificationService;
    private readonly StreakService? _streakService;

    private ObservableCollection<TodoTask> _tasks = new();
    private int _currentStreak;
    private string _streakText = "💔 0 Days";
    private string _newTaskTitle = string.Empty;
    private DateTime? _newTaskDueDate;
    private TaskPriority _newTaskPriority = TaskPriority.Medium;
    private bool _hasTasks;
    public event Action? RequestOpenDashboard;
    public ObservableCollection<TodoTask> Tasks
    {
        get => _tasks;
        set
        {
            if (SetProperty(ref _tasks, value))
            {
                HasTasks = _tasks.Count > 0;
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
                    ? $"🔥 {value} {(value == 1 ? "Day" : "Days")}"
                    : "💔 0 Days";
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
    public IEnumerable<TaskPriority> Priorities => Enum.GetValues<TaskPriority>();
    public bool HasTasks
    {
        get => _hasTasks;
        private set => SetProperty(ref _hasTasks, value);
    }
    public RelayCommand<TodoTask> CompleteTaskCommand { get; }
    public RelayCommand<TodoTask> DeleteTaskCommand { get; }
    public RelayCommand<TodoTask> ToggleVoiceNoteCommand { get; }
    public RelayCommand OpenDashboardCommand { get; }
    public RelayCommand AddQuickTaskCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public WidgetViewModel(
        AppDatabase database,
        NotificationService? notificationService = null,
        StreakService? streakService = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _notificationService = notificationService;
        _streakService = streakService;

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

        if (_notificationService != null)
        {
            _notificationService.TasksChanged += OnBackgroundTasksChanged;
        }
        NotificationService.StaticTasksChanged += OnBackgroundTasksChanged;

        if (_streakService != null)
        {
            _streakService.StreakChanged += OnBackgroundStreakChanged;
        }

        RefreshTasks();
        RefreshStreak();
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
                $"Are you sure this task is completed?\n\n\"{task.Title}\"",
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
    }

    private void ExecuteDeleteTask(TodoTask? task)
    {
        if (task == null) return;

        _database.DeleteTask(task.Id);
        RefreshTasks();
        RefreshStreak();
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
