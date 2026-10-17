namespace TodoWidget.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;
using TodoWidget.Services;
public class DashboardViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly StreakService? _streakService;

    private ObservableCollection<TodoTask> _allTasks = new();
    private ObservableCollection<TodoTask> _filteredTasks = new();
    private string _filterText = string.Empty;
    private TodoTaskStatus? _statusFilter;
    private TaskPriority? _priorityFilter;
    private TodoTask? _selectedTask;
    private int _selectedTabIndex;
    private int _totalTasks;
    private int _completedToday;
    private int _pendingCount;
    private int _streakCount;
    private int _longestStreak;
    private int _totalCompleted;
    private int _totalFailed;
    private double _completionRate;
    public event Action? RequestAddTask;
    public event Action<TodoTask>? RequestEditTask;
    public SettingsViewModel SettingsVM { get; }
    public ObservableCollection<TodoTask> AllTasks
    {
        get => _allTasks;
        set => SetProperty(ref _allTasks, value);
    }
    public ObservableCollection<TodoTask> FilteredTasks
    {
        get => _filteredTasks;
        set => SetProperty(ref _filteredTasks, value);
    }
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                ApplyFilters();
            }
        }
    }
    public TodoTaskStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }
    public TaskPriority? PriorityFilter
    {
        get => _priorityFilter;
        set
        {
            if (SetProperty(ref _priorityFilter, value))
            {
                ApplyFilters();
            }
        }
    }
    public TodoTask? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    public int TotalTasks
    {
        get => _totalTasks;
        set => SetProperty(ref _totalTasks, value);
    }
    public int CompletedToday
    {
        get => _completedToday;
        set => SetProperty(ref _completedToday, value);
    }
    public int PendingCount
    {
        get => _pendingCount;
        set => SetProperty(ref _pendingCount, value);
    }
    public int StreakCount
    {
        get => _streakCount;
        set => SetProperty(ref _streakCount, value);
    }
    public int LongestStreak
    {
        get => _longestStreak;
        set => SetProperty(ref _longestStreak, value);
    }
    public int TotalCompleted
    {
        get => _totalCompleted;
        set => SetProperty(ref _totalCompleted, value);
    }
    public int TotalFailed
    {
        get => _totalFailed;
        set => SetProperty(ref _totalFailed, value);
    }
    public double CompletionRate
    {
        get => _completionRate;
        set => SetProperty(ref _completionRate, value);
    }
    public RelayCommand AddTaskCommand { get; }
    public RelayCommand<TodoTask> EditTaskCommand { get; }
    public RelayCommand<TodoTask> DeleteTaskCommand { get; }
    public RelayCommand<TodoTask> ToggleTaskStatusCommand { get; }
    public RelayCommand<TodoTask> ToggleVoiceNoteCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public string? CurrentlyPlayingVoiceNotePath => AudioService.Instance.CurrentPlayingPath;
    public DashboardViewModel(AppDatabase database, StreakService? streakService = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _streakService = streakService;

        SettingsVM = new SettingsViewModel(database);

        AddTaskCommand = new RelayCommand(() => RequestAddTask?.Invoke());
        EditTaskCommand = new RelayCommand<TodoTask>(task =>
        {
            var target = task ?? SelectedTask;
            if (target != null)
            {
                RequestEditTask?.Invoke(target);
            }
        });
        DeleteTaskCommand = new RelayCommand<TodoTask>(ExecuteDeleteTask);
        ToggleTaskStatusCommand = new RelayCommand<TodoTask>(ExecuteToggleStatus);
        ToggleVoiceNoteCommand = new RelayCommand<TodoTask>(task =>
        {
            if (task != null && task.HasVoiceNote)
            {
                AudioService.Instance.TogglePlay(task.VoiceNotePath);
            }
        });
        OpenSettingsCommand = new RelayCommand(() => SelectedTabIndex = 2);
        RefreshCommand = new RelayCommand(RefreshAll);

        AudioService.Instance.PlaybackStateChanged += () =>
        {
            RunOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(CurrentlyPlayingVoiceNotePath));
            });
        };

        RefreshAll();
    }
    public void RefreshAll()
    {
        RunOnDispatcher(() =>
        {
            var tasks = _database.GetAllTasks() ?? new();
            AllTasks = new ObservableCollection<TodoTask>(tasks);
            TotalTasks = tasks.Count;
            CompletedToday = tasks.Count(t => t.Status == TodoTaskStatus.Completed && t.CompletedAt?.Date == DateTime.Today);
            PendingCount = tasks.Count(t => t.Status == TodoTaskStatus.Pending);

            StreakData? streakData = _streakService != null
                ? _streakService.GetCurrentStreak()
                : _database.GetStreakData();

            if (streakData != null)
            {
                StreakCount = streakData.CurrentStreak;
                LongestStreak = streakData.LongestStreak;
                TotalCompleted = streakData.TotalTasksCompleted;
                TotalFailed = streakData.TotalTasksFailed;
            }
            else
            {
                StreakCount = 0;
                LongestStreak = 0;
                TotalCompleted = tasks.Count(t => t.Status == TodoTaskStatus.Completed);
                TotalFailed = tasks.Count(t => t.Status == TodoTaskStatus.Failed);
            }

            int resolvedTotal = TotalCompleted + TotalFailed;
            CompletionRate = resolvedTotal > 0 ? Math.Round((double)TotalCompleted / resolvedTotal * 100.0, 1) : 0;

            ApplyFilters();
        });
    }

    private void ApplyFilters()
    {
        var query = AllTasks.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            string search = FilterText.Trim().ToLowerInvariant();
            query = query.Where(t =>
                (t.Title != null && t.Title.ToLowerInvariant().Contains(search)) ||
                (t.Description != null && t.Description.ToLowerInvariant().Contains(search)));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(t => t.Status == StatusFilter.Value);
        }

        if (PriorityFilter.HasValue)
        {
            query = query.Where(t => t.Priority == PriorityFilter.Value);
        }

        var ordered = query
            .OrderBy(t => t.Status == TodoTaskStatus.Completed ? 1 : 0)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.EffectiveDueDate ?? DateTime.MaxValue)
            .ToList();

        FilteredTasks = new ObservableCollection<TodoTask>(ordered);
    }

    private void ExecuteDeleteTask(TodoTask? task)
    {
        var target = task ?? SelectedTask;
        if (target == null) return;

        _database.DeleteTask(target.Id);
        RefreshAll();
    }

    private void ExecuteToggleStatus(TodoTask? task)
    {
        var target = task ?? SelectedTask;
        if (target == null) return;

        if (target.Status == TodoTaskStatus.Completed)
        {
            target.Status = TodoTaskStatus.Pending;
            target.CompletedAt = null;
        }
        else
        {
            var result = MessageBox.Show(
                $"Are you sure this task is completed?\n\n\"{target.Title}\"",
                "Complete Task",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            target.Status = TodoTaskStatus.Completed;
            target.CompletedAt = DateTime.Now;
        }

        _database.UpsertTask(target);
        RefreshAll();
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
