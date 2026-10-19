namespace TodoWidget.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;
using TodoWidget.Services;
public class SettingsViewModel : ObservableObject
{
    private readonly AppDatabase? _database;

    private string _widgetBackgroundColor = "#1E1E2E";
    private string _widgetForegroundColor = "#CDD6F4";
    private string _accentColor = "#3B82F6";
    private double _widgetOpacity = 0.95;
    private string _fontFamily = "Segoe UI";
    private double _fontSize = 13.0;
    private double _widgetWidth = 250.0;
    private double _widgetHeight = 250.0;
    private double _widgetCornerRadius = 14.0;
    private double _widgetLeft = 100.0;
    private double _widgetTop = 100.0;
    private WidgetMode _widgetMode = WidgetMode.FloatOnTop;
    private WidgetDockPosition _dockPosition = WidgetDockPosition.TopRight;
    private bool _widgetVisible = true;
    private AppTheme _theme = AppTheme.Dark;

    private bool _notificationsEnabled = true;
    private bool _notificationSoundEnabled = true;
    private int _defaultSnoozeDurationMinutes = 10;
    private bool _launchOnStartup;
    private bool _startMinimized;
    private bool _showStreakOnWidget = true;
    private int _dayEndHour = 24;

    private ObservableCollection<int> _reminderIntervals = new();
    private int _newReminderMinutes = 15;
    private string _statusMessage = string.Empty;
    private bool _checkForUpdatesOnStartup = true;
    private bool _enableAnimations = true;
    private bool _autoSyncEnabled = true;
    private int _focusSprintMinutes = 2;
    private int _focusPomodoroMinutes = 25;
    private bool _appBlockingEnabled = true;
    private bool _blockTaskManagerInFocus = true;
    private bool _strictFocusLock = false;

    public bool AutoSyncEnabled
    {
        get => _autoSyncEnabled;
        set => SetProperty(ref _autoSyncEnabled, value);
    }

    public int FocusSprintMinutes
    {
        get => _focusSprintMinutes;
        set => SetProperty(ref _focusSprintMinutes, Math.Clamp(value, 1, 30));
    }

    public int FocusPomodoroMinutes
    {
        get => _focusPomodoroMinutes;
        set => SetProperty(ref _focusPomodoroMinutes, Math.Clamp(value, 5, 90));
    }

    public bool AppBlockingEnabled
    {
        get => _appBlockingEnabled;
        set => SetProperty(ref _appBlockingEnabled, value);
    }

    public bool BlockTaskManagerInFocus
    {
        get => _blockTaskManagerInFocus;
        set => SetProperty(ref _blockTaskManagerInFocus, value);
    }

    public bool StrictFocusLock
    {
        get => _strictFocusLock;
        set => SetProperty(ref _strictFocusLock, value);
    }

    public UpdateService Updater => UpdateService.Instance;
    public string CurrentVersionText => UpdateService.CurrentVersionDisplay;
    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set => SetProperty(ref _checkForUpdatesOnStartup, value);
    }
    public RelayCommand CheckForUpdatesCommand { get; }
    public RelayCommand InstallUpdateCommand { get; }
    public IEnumerable<AppTheme> AvailableThemes => Enum.GetValues<AppTheme>();
    public IEnumerable<WidgetDockPosition> AvailableDockPositions => Enum.GetValues<WidgetDockPosition>();
    public ObservableCollection<string> AvailableFonts { get; } = new()
    {
        "Segoe UI",
        "Segoe UI Variable Display",
        "Aptos",
        "Calibri",
        "Consolas",
        "Arial",
        "Tahoma"
    };
    public ObservableCollection<int> SnoozeOptions { get; } = new() { 5, 10, 15, 30, 60 };
    public string WidgetBackgroundColor
    {
        get => _widgetBackgroundColor;
        set => SetProperty(ref _widgetBackgroundColor, value);
    }
    public string WidgetForegroundColor
    {
        get => _widgetForegroundColor;
        set => SetProperty(ref _widgetForegroundColor, value);
    }
    public string AccentColor
    {
        get => _accentColor;
        set => SetProperty(ref _accentColor, value);
    }
    public double WidgetOpacity
    {
        get => _widgetOpacity;
        set => SetProperty(ref _widgetOpacity, Math.Clamp(value, 0.2, 1.0));
    }
    public string FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }
    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, Math.Clamp(value, 10.0, 24.0));
    }
    public double WidgetWidth
    {
        get => _widgetWidth;
        set => SetProperty(ref _widgetWidth, Math.Max(160.0, value));
    }
    public double WidgetHeight
    {
        get => _widgetHeight;
        set => SetProperty(ref _widgetHeight, Math.Max(160.0, value));
    }
    public WidgetDockPosition DockPosition
    {
        get => _dockPosition;
        set => SetProperty(ref _dockPosition, value);
    }
    public double WidgetCornerRadius
    {
        get => _widgetCornerRadius;
        set => SetProperty(ref _widgetCornerRadius, Math.Clamp(value, 0.0, 32.0));
    }
    public double WidgetLeft
    {
        get => _widgetLeft;
        set => SetProperty(ref _widgetLeft, value);
    }
    public double WidgetTop
    {
        get => _widgetTop;
        set => SetProperty(ref _widgetTop, value);
    }
    public WidgetMode WidgetMode
    {
        get => _widgetMode;
        set
        {
            if (SetProperty(ref _widgetMode, value))
            {
                OnPropertyChanged(nameof(IsFloatOnTop));
                OnPropertyChanged(nameof(IsPinnedToDesktop));
            }
        }
    }
    public bool IsFloatOnTop
    {
        get => WidgetMode == WidgetMode.FloatOnTop;
        set
        {
            if (value)
            {
                WidgetMode = WidgetMode.FloatOnTop;
            }
        }
    }
    public bool IsPinnedToDesktop
    {
        get => WidgetMode == WidgetMode.PinnedToDesktop;
        set
        {
            if (value)
            {
                WidgetMode = WidgetMode.PinnedToDesktop;
            }
        }
    }
    public bool WidgetVisible
    {
        get => _widgetVisible;
        set => SetProperty(ref _widgetVisible, value);
    }
    public AppTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }
    public bool NotificationSoundEnabled
    {
        get => _notificationSoundEnabled;
        set => SetProperty(ref _notificationSoundEnabled, value);
    }
    public int DefaultSnoozeDurationMinutes
    {
        get => _defaultSnoozeDurationMinutes;
        set => SetProperty(ref _defaultSnoozeDurationMinutes, value);
    }
    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set
        {
            if (SetProperty(ref _launchOnStartup, value))
            {
                try
                {
                    StartupManager.SetStartupEnabled(value);
                }
                catch
                {
                }
            }
        }
    }
    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }
    public bool EnableAnimations
    {
        get => _enableAnimations;
        set => SetProperty(ref _enableAnimations, value);
    }
    public bool ShowStreakOnWidget
    {
        get => _showStreakOnWidget;
        set => SetProperty(ref _showStreakOnWidget, value);
    }
    public int DayEndHour
    {
        get => _dayEndHour;
        set => SetProperty(ref _dayEndHour, Math.Clamp(value, 0, 24));
    }
    public ObservableCollection<int> ReminderIntervals
    {
        get => _reminderIntervals;
        set => SetProperty(ref _reminderIntervals, value);
    }
    public int NewReminderMinutes
    {
        get => _newReminderMinutes;
        set => SetProperty(ref _newReminderMinutes, Math.Max(1, value));
    }
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetDefaultsCommand { get; }
    public RelayCommand AddReminderCommand { get; }
    public RelayCommand<int> RemoveReminderCommand { get; }
    public SettingsViewModel(AppDatabase? database = null)
    {
        _database = database;

        SaveCommand = new RelayCommand(ExecuteSave);
        ResetDefaultsCommand = new RelayCommand(ExecuteResetDefaults);
        AddReminderCommand = new RelayCommand(ExecuteAddReminder);
        RemoveReminderCommand = new RelayCommand<int>(ExecuteRemoveReminder);
        CheckForUpdatesCommand = new RelayCommand(async () =>
        {
            await UpdateService.Instance.CheckForUpdatesAsync(manualCheck: true);
        });
        InstallUpdateCommand = new RelayCommand(async () =>
        {
            if (UpdateService.Instance.AvailableUpdate != null)
            {
                var update = UpdateService.Instance.AvailableUpdate;
                var res = System.Windows.MessageBox.Show(
                    $"Download and install To-Do {update.DisplayVersion}?\n\nRelease Title: {update.Title}\n\nRelease Notes:\n{update.ReleaseNotes}",
                    "Update Available",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    await UpdateService.Instance.DownloadAndInstallAsync(update);
                }
            }
        });

        LoadSettings();
    }
    public void LoadSettings()
    {
        AppSettings? settings = SettingsManager.Instance?.Current ?? _database?.GetSettings();
        if (settings == null) return;

        WidgetBackgroundColor = settings.WidgetBackgroundColor ?? "#1E1E2E";
        WidgetForegroundColor = settings.WidgetForegroundColor ?? "#CDD6F4";
        AccentColor = settings.AccentColor ?? "#3B82F6";
        WidgetOpacity = settings.WidgetOpacity > 0 ? settings.WidgetOpacity : 0.95;
        FontFamily = !string.IsNullOrEmpty(settings.FontFamily) ? settings.FontFamily : "Segoe UI";
        FontSize = settings.FontSize > 0 ? settings.FontSize : 13.0;
        WidgetWidth = settings.WidgetWidth > 0 ? settings.WidgetWidth : 250.0;
        WidgetHeight = settings.WidgetHeight > 0 ? settings.WidgetHeight : 250.0;
        WidgetCornerRadius = settings.WidgetCornerRadius >= 0 ? settings.WidgetCornerRadius : 14.0;
        WidgetLeft = settings.WidgetLeft;
        WidgetTop = settings.WidgetTop;
        WidgetMode = settings.WidgetMode;
        DockPosition = settings.DockPosition;
        WidgetVisible = settings.WidgetVisible;
        Theme = settings.Theme;

        NotificationsEnabled = settings.NotificationsEnabled;
        NotificationSoundEnabled = settings.NotificationSoundEnabled;
        DefaultSnoozeDurationMinutes = settings.DefaultSnoozeDurationMinutes > 0 ? settings.DefaultSnoozeDurationMinutes : 10;
        _launchOnStartup = settings.LaunchOnStartup;
        StartMinimized = settings.StartMinimized;
        EnableAnimations = settings.EnableAnimations;
        ShowStreakOnWidget = settings.ShowStreakOnWidget;
        DayEndHour = settings.DayEndHour;
        CheckForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        AutoSyncEnabled = settings.AutoSyncEnabled;
        FocusSprintMinutes = settings.FocusSprintMinutes > 0 ? settings.FocusSprintMinutes : 2;
        FocusPomodoroMinutes = settings.FocusPomodoroMinutes > 0 ? settings.FocusPomodoroMinutes : 25;
        AppBlockingEnabled = settings.AppBlockingEnabled;
        BlockTaskManagerInFocus = settings.BlockTaskManagerInFocus;
        StrictFocusLock = settings.StrictFocusLock;

        var intervals = settings.ReminderIntervalsMinutes != null && settings.ReminderIntervalsMinutes.Count > 0
            ? settings.ReminderIntervalsMinutes
            : new List<int> { 5, 15, 30 };

        ReminderIntervals = new ObservableCollection<int>(intervals.OrderBy(i => i));
    }

    private void ExecuteSave()
    {
        AppSettings settings = SettingsManager.Instance?.Current ?? _database?.GetSettings() ?? new AppSettings();

        settings.WidgetBackgroundColor = WidgetBackgroundColor;
        settings.WidgetForegroundColor = WidgetForegroundColor;
        settings.AccentColor = AccentColor;
        settings.WidgetOpacity = WidgetOpacity;
        settings.FontFamily = FontFamily;
        settings.FontSize = FontSize;
        settings.WidgetWidth = WidgetWidth;
        settings.WidgetHeight = WidgetHeight;
        settings.WidgetCornerRadius = WidgetCornerRadius;
        settings.WidgetLeft = WidgetLeft;
        settings.WidgetTop = WidgetTop;
        settings.WidgetMode = WidgetMode;
        settings.DockPosition = DockPosition;
        settings.WidgetVisible = WidgetVisible;
        settings.Theme = Theme;

        settings.NotificationsEnabled = NotificationsEnabled;
        settings.NotificationSoundEnabled = NotificationSoundEnabled;
        settings.DefaultSnoozeDurationMinutes = DefaultSnoozeDurationMinutes;
        settings.LaunchOnStartup = LaunchOnStartup;
        settings.StartMinimized = StartMinimized;
        settings.EnableAnimations = EnableAnimations;
        settings.ShowStreakOnWidget = ShowStreakOnWidget;
        settings.DayEndHour = DayEndHour;
        settings.CheckForUpdatesOnStartup = CheckForUpdatesOnStartup;
        settings.AutoSyncEnabled = AutoSyncEnabled;
        settings.FocusSprintMinutes = FocusSprintMinutes;
        settings.FocusPomodoroMinutes = FocusPomodoroMinutes;
        settings.AppBlockingEnabled = AppBlockingEnabled;
        settings.BlockTaskManagerInFocus = BlockTaskManagerInFocus;
        settings.StrictFocusLock = StrictFocusLock;
        settings.ReminderIntervalsMinutes = ReminderIntervals.ToList();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.Save();
        }
        else if (_database != null)
        {
            _database.SaveSettings(settings);
        }

        StatusMessage = "Settings saved successfully.";
    }

    private void ExecuteResetDefaults()
    {
        WidgetBackgroundColor = "#1E1E2E";
        WidgetForegroundColor = "#CDD6F4";
        AccentColor = "#3B82F6";
        WidgetOpacity = 0.95;
        FontFamily = "Segoe UI";
        FontSize = 13.0;
        WidgetWidth = 250.0;
        WidgetHeight = 250.0;
        WidgetCornerRadius = 14.0;
        WidgetMode = WidgetMode.FloatOnTop;
        DockPosition = WidgetDockPosition.TopRight;
        WidgetVisible = true;
        Theme = AppTheme.Dark;

        NotificationsEnabled = true;
        NotificationSoundEnabled = true;
        DefaultSnoozeDurationMinutes = 10;
        LaunchOnStartup = false;
        StartMinimized = false;
        EnableAnimations = true;
        ShowStreakOnWidget = true;
        DayEndHour = 24;
        CheckForUpdatesOnStartup = true;
        AutoSyncEnabled = true;
        FocusSprintMinutes = 2;
        FocusPomodoroMinutes = 25;

        ReminderIntervals = new ObservableCollection<int> { 5, 15, 30 };

        ExecuteSave();
        StatusMessage = "Settings reset to defaults.";
    }

    private void ExecuteAddReminder()
    {
        if (NewReminderMinutes > 0 && !ReminderIntervals.Contains(NewReminderMinutes))
        {
            var list = ReminderIntervals.Concat(new[] { NewReminderMinutes }).OrderBy(i => i).ToList();
            ReminderIntervals = new ObservableCollection<int>(list);
        }
    }

    private void ExecuteRemoveReminder(int minutes)
    {
        ReminderIntervals.Remove(minutes);
    }
}
