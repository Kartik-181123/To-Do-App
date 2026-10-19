namespace TodoWidget.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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

    // GitHub Cloud Sync & Account
    public GitHubSyncService CloudSync => GitHubSyncService.Instance;
    private string _gitHubTokenInput = string.Empty;
    public string GitHubTokenInput
    {
        get => _gitHubTokenInput;
        set => SetProperty(ref _gitHubTokenInput, value);
    }
    public RelayCommand ConnectGitHubCommand { get; }
    public RelayCommand DisconnectGitHubCommand { get; }
    public RelayCommand SyncNowCommand { get; }

    public string? CurrentlyPlayingVoiceNotePath => AudioService.Instance.CurrentPlayingPath;

    // App Blocker & Focus Suite Properties
    private ObservableCollection<BlockedAppInfo> _blockedApps = new();
    private ObservableCollection<BlockedAppInfo> _scannedApps = new();
    private ObservableCollection<BlockedAppInfo> _filteredScannedApps = new();
    private ObservableCollection<BlockedAppInfo> _distractionPresets = new();
    private string _scannedSearchQuery = string.Empty;
    private bool _isScanningApps;
    private bool _hasScannedApps;

    public ObservableCollection<BlockedAppInfo> BlockedApps
    {
        get => _blockedApps;
        set => SetProperty(ref _blockedApps, value);
    }

    public ObservableCollection<BlockedAppInfo> FilteredScannedApps
    {
        get => _filteredScannedApps;
        set => SetProperty(ref _filteredScannedApps, value);
    }

    public ObservableCollection<BlockedAppInfo> DistractionPresets
    {
        get => _distractionPresets;
        set => SetProperty(ref _distractionPresets, value);
    }

    public string ScannedSearchQuery
    {
        get => _scannedSearchQuery;
        set
        {
            if (SetProperty(ref _scannedSearchQuery, value))
            {
                ApplyScannedAppsFilter();
            }
        }
    }

    public bool IsScanningApps
    {
        get => _isScanningApps;
        set => SetProperty(ref _isScanningApps, value);
    }

    public bool HasScannedApps
    {
        get => _hasScannedApps;
        set => SetProperty(ref _hasScannedApps, value);
    }

    public bool AppBlockingEnabled
    {
        get => SettingsManager.Instance.Current.AppBlockingEnabled;
        set
        {
            var settings = SettingsManager.Instance.Current;
            if (settings.AppBlockingEnabled != value)
            {
                settings.AppBlockingEnabled = value;
                SettingsManager.Instance.Save();
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppBlockingStatusText));
            }
        }
    }

    public bool BlockTaskManagerInFocus
    {
        get => SettingsManager.Instance.Current.BlockTaskManagerInFocus;
        set
        {
            var settings = SettingsManager.Instance.Current;
            if (settings.BlockTaskManagerInFocus != value)
            {
                settings.BlockTaskManagerInFocus = value;
                SettingsManager.Instance.Save();
                OnPropertyChanged();
            }
        }
    }

    public bool StrictFocusLock
    {
        get => SettingsManager.Instance.Current.StrictFocusLock;
        set
        {
            var settings = SettingsManager.Instance.Current;
            if (settings.StrictFocusLock != value)
            {
                settings.StrictFocusLock = value;
                SettingsManager.Instance.Save();
                OnPropertyChanged();
            }
        }
    }

    public bool IsFocusModeActive
    {
        get => SettingsManager.Instance.Current.IsFocusModeActive;
        set
        {
            var settings = SettingsManager.Instance.Current;
            if (settings.IsFocusModeActive != value)
            {
                settings.IsFocusModeActive = value;
                SettingsManager.Instance.Save();
                if (value)
                {
                    AppBlockerService.Instance.Start();
                }
                else
                {
                    AppBlockerService.Instance.Stop();
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(FocusModeStatusText));
                OnPropertyChanged(nameof(FocusModeButtonText));
            }
        }
    }

    public int BlockedCountThisSession => AppBlockerService.Instance.BlockedCountThisSession;
    public string FocusModeStatusText => IsFocusModeActive ? "Focus Mode Active" : "Focus Mode Inactive";
    public string FocusModeButtonText => IsFocusModeActive ? "Stop Focus Session" : "Start Focus Session";
    public string AppBlockingStatusText => AppBlockingEnabled ? "Enforcement Active" : "Disabled";

    public RelayCommand ToggleFocusModeCommand { get; }
    public RelayCommand ScanInstalledAppsCommand { get; }
    public RelayCommand<BlockedAppInfo> AddBlockedAppCommand { get; }
    public RelayCommand<BlockedAppInfo> RemoveBlockedAppCommand { get; }
    public RelayCommand<BlockedAppInfo> ToggleBlockedAppStatusCommand { get; }
    public RelayCommand<string> AddCustomAppFromFileCommand { get; }
    public RelayCommand ClearAllBlockedAppsCommand { get; }
    public RelayCommand ResetSessionBlockCountCommand { get; }

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
        OpenSettingsCommand = new RelayCommand(() => SelectedTabIndex = 4);
        RefreshCommand = new RelayCommand(RefreshAll);

        ConnectGitHubCommand = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(GitHubTokenInput))
            {
                bool success = await CloudSync.ConnectAccountAsync(GitHubTokenInput);
                if (success)
                {
                    GitHubTokenInput = string.Empty;
                    RefreshAll();
                }
            }
        });

        DisconnectGitHubCommand = new RelayCommand(() =>
        {
            CloudSync.DisconnectAccount();
        });

        SyncNowCommand = new RelayCommand(async () =>
        {
            await CloudSync.SyncNowAsync();
            RefreshAll();
        });

        GitHubSyncService.StaticSyncCompleted += () =>
        {
            RunOnDispatcher(RefreshAll);
        };

        AudioService.Instance.PlaybackStateChanged += () =>
        {
            RunOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(CurrentlyPlayingVoiceNotePath));
            });
        };

        ToggleFocusModeCommand = new RelayCommand(() =>
        {
            IsFocusModeActive = !IsFocusModeActive;
        });

        ScanInstalledAppsCommand = new RelayCommand(async () =>
        {
            await ScanInstalledAppsAsync();
        });

        AddBlockedAppCommand = new RelayCommand<BlockedAppInfo>(app =>
        {
            if (app != null)
            {
                AddAppToBlockedList(app);
            }
        });

        RemoveBlockedAppCommand = new RelayCommand<BlockedAppInfo>(app =>
        {
            if (app != null)
            {
                RemoveAppFromBlockedList(app);
            }
        });

        ToggleBlockedAppStatusCommand = new RelayCommand<BlockedAppInfo>(app =>
        {
            if (app != null)
            {
                app.IsEnabled = !app.IsEnabled;
                SaveBlockedApps();
                OnPropertyChanged(nameof(BlockedApps));
            }
        });

        AddCustomAppFromFileCommand = new RelayCommand<string>(path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var app = AppScannerService.Instance.CreateFromFile(path);
                if (app != null)
                {
                    AddAppToBlockedList(app);
                }
            }
        });

        ClearAllBlockedAppsCommand = new RelayCommand(() =>
        {
            if (BlockedApps.Count == 0) return;
            var res = MessageBox.Show(
                "Are you sure you want to clear all blocked applications?",
                "Clear Blocked Apps",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                BlockedApps.Clear();
                SaveBlockedApps();
            }
        });

        ResetSessionBlockCountCommand = new RelayCommand(() =>
        {
            AppBlockerService.Instance.ResetSessionCount();
            OnPropertyChanged(nameof(BlockedCountThisSession));
        });

        AppBlockerService.Instance.BlockedCountChanged += () =>
        {
            RunOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(BlockedCountThisSession));
            });
        };

        LoadBlockedApps();
        LoadDistractionPresets();

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

        target.UpdatedAt = DateTime.Now;
        _database.UpsertTask(target);
        RefreshAll();

        if (GitHubSyncService.Instance.IsConnected)
        {
            _ = Task.Run(async () => await GitHubSyncService.Instance.SyncNowAsync().ConfigureAwait(false));
        }
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

    private void LoadBlockedApps()
    {
        var settings = SettingsManager.Instance.Current;
        settings.BlockedApps ??= new List<BlockedAppInfo>();

        foreach (var app in settings.BlockedApps)
        {
            if (app.Icon == null && !string.IsNullOrWhiteSpace(app.ExecutablePath))
            {
                app.Icon = AppScannerService.TryExtractIcon(app.ExecutablePath);
            }
        }

        BlockedApps = new ObservableCollection<BlockedAppInfo>(settings.BlockedApps);
    }

    private void LoadDistractionPresets()
    {
        var presets = AppScannerService.Instance.GetCommonDistractionPresets();
        DistractionPresets = new ObservableCollection<BlockedAppInfo>(presets);
    }

    public void SaveBlockedApps()
    {
        var settings = SettingsManager.Instance.Current;
        settings.BlockedApps = BlockedApps.ToList();
        SettingsManager.Instance.Save();
        OnPropertyChanged(nameof(BlockedApps));
    }

    public void AddAppToBlockedList(BlockedAppInfo app)
    {
        if (app == null) return;

        // Prevent duplicate process entries
        if (BlockedApps.Any(a => string.Equals(a.ProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var copy = new BlockedAppInfo
        {
            Name = app.Name,
            ProcessName = app.ProcessName,
            ExecutablePath = app.ExecutablePath,
            IsEnabled = true,
            IsCustom = app.IsCustom,
            AddedAt = DateTime.Now,
            Icon = app.Icon ?? (string.IsNullOrWhiteSpace(app.ExecutablePath) ? null : AppScannerService.TryExtractIcon(app.ExecutablePath))
        };

        BlockedApps.Add(copy);
        SaveBlockedApps();
    }

    public void RemoveAppFromBlockedList(BlockedAppInfo app)
    {
        if (app == null) return;
        var existing = BlockedApps.FirstOrDefault(a =>
            string.Equals(a.ProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            BlockedApps.Remove(existing);
            SaveBlockedApps();
        }
    }

    public async Task ScanInstalledAppsAsync()
    {
        if (IsScanningApps) return;

        IsScanningApps = true;
        try
        {
            var apps = await AppScannerService.Instance.ScanInstalledAppsAsync();
            _scannedApps = new ObservableCollection<BlockedAppInfo>(apps);
            HasScannedApps = true;
            ApplyScannedAppsFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Error scanning apps: {ex.Message}");
        }
        finally
        {
            IsScanningApps = false;
        }
    }

    private void ApplyScannedAppsFilter()
    {
        if (_scannedApps == null || _scannedApps.Count == 0)
        {
            FilteredScannedApps = new ObservableCollection<BlockedAppInfo>();
            return;
        }

        if (string.IsNullOrWhiteSpace(ScannedSearchQuery))
        {
            FilteredScannedApps = new ObservableCollection<BlockedAppInfo>(_scannedApps);
            return;
        }

        string q = ScannedSearchQuery.Trim().ToLowerInvariant();
        var matches = _scannedApps.Where(a =>
            (!string.IsNullOrEmpty(a.Name) && a.Name.ToLowerInvariant().Contains(q)) ||
            (!string.IsNullOrEmpty(a.ProcessName) && a.ProcessName.ToLowerInvariant().Contains(q)));

        FilteredScannedApps = new ObservableCollection<BlockedAppInfo>(matches);
    }
}
