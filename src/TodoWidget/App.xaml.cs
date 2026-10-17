namespace TodoWidget;

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using TodoWidget.Data;
using TodoWidget.Services;
using TodoWidget.ViewModels;
using TodoWidget.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
public partial class App : Application
{
    private const string MutexName = "TodoWidget_SingleInstance";
    private Mutex? _singleInstanceMutex;

    private NotifyIcon? _trayIcon;
    private ToolStripMenuItem? _showWidgetMenuItem;

    private WidgetWindow? _widgetWindow;
    private DashboardWindow? _dashboardWindow;

    private AppDatabase? _database;
    private NotificationService? _notificationService;
    private ReminderService? _reminderService;
    private StreakService? _streakService;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "To-Do is already running. Please check your system tray.",
                "To-Do",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Shutdown();
            return;
        }
        _database = AppDatabase.Instance;
        SettingsManager.Instance.SetDatabase(_database);
        var settings = SettingsManager.Instance.Load();
        _notificationService = new NotificationService(_database, SettingsManager.Instance);
        _notificationService.Initialize();
        _reminderService = new ReminderService(_database, SettingsManager.Instance, _notificationService);
        _reminderService.Start();
        _streakService = new StreakService(_database, SettingsManager.Instance, _notificationService);
        _streakService.CheckAndEvaluateYesterday();
        CreateTrayIcon();
        InitializeWidgetWindow();
        if (settings.WidgetVisible)
        {
            _widgetWindow?.Show();
        }

        bool runSilent = e.Args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase) || 
                                         a.Equals("-silent", StringComparison.OrdinalIgnoreCase) ||
                                         a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                                         a.Equals("-minimized", StringComparison.OrdinalIgnoreCase));

        if (!runSilent && (!settings.StartMinimized || !settings.WidgetVisible))
        {
            OpenDashboard();
        }

        if (settings.CheckForUpdatesOnStartup)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                await UpdateService.Instance.CheckForUpdatesAsync(manualCheck: false);
            });
        }
    }
    public void OpenDashboard()
    {
        if (_database == null) return;

        if (_dashboardWindow == null)
        {
            var dashboardVm = new DashboardViewModel(_database, _streakService);
            _dashboardWindow = new DashboardWindow(dashboardVm, _database);
        }

        _dashboardWindow.Show();
        if (_dashboardWindow.WindowState == WindowState.Minimized)
        {
            _dashboardWindow.WindowState = WindowState.Normal;
        }

        _dashboardWindow.Activate();
        _dashboardWindow.Focus();
    }
    public void ToggleWidgetVisibility()
    {
        var settings = SettingsManager.Instance.Current;
        settings.WidgetVisible = !settings.WidgetVisible;
        SettingsManager.Instance.Save();

        if (_showWidgetMenuItem != null)
        {
            _showWidgetMenuItem.Checked = settings.WidgetVisible;
        }

        if (settings.WidgetVisible)
        {
            if (_widgetWindow == null)
            {
                InitializeWidgetWindow();
            }
            _widgetWindow?.Show();
        }
        else
        {
            _widgetWindow?.Hide();
        }
    }

    private void InitializeWidgetWindow()
    {
        if (_database == null) return;

        var widgetVm = new WidgetViewModel(_database, _notificationService, _streakService);
        widgetVm.RequestOpenDashboard += OpenDashboard;

        _widgetWindow = new WidgetWindow(widgetVm);
    }

    private void CreateTrayIcon()
    {
        var settings = SettingsManager.Instance.Current;

        var contextMenu = new ContextMenuStrip();

        _showWidgetMenuItem = new ToolStripMenuItem("Show Widget", null, (s, e) => ToggleWidgetVisibility())
        {
            Checked = settings.WidgetVisible
        };

        var dashboardMenuItem = new ToolStripMenuItem("Open Dashboard", null, (s, e) => OpenDashboard());
        var exitMenuItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(_showWidgetMenuItem);
        contextMenu.Items.Add(dashboardMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        _trayIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "To-Do",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _trayIcon.DoubleClick += (s, e) => OpenDashboard();
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            string[] candidatePaths = [
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "taskbar-icon.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "taskbar-icon.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "taskbar-icon.png")
            ];

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    using var bmp = new Bitmap(path);
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
        }
        catch
        {
        }

        return SystemIcons.Application;
    }

    private void ExitApplication()
    {
        Teardown();
        Shutdown();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Teardown();
        base.OnExit(e);
    }

    private void Teardown()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _reminderService?.Stop();
        _reminderService?.Dispose();
        _reminderService = null;

        _streakService?.Dispose();
        _streakService = null;

        _database?.Dispose();
        _database = null;

        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch
            {
            }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }
}
