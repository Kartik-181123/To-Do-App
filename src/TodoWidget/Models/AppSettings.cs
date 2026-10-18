using LiteDB;

namespace TodoWidget.Models;
public class AppSettings
{
    [BsonId]
    public int Id { get; set; } = 1;
    public string WidgetBackgroundColor { get; set; } = "#1E1E2E";
    public string WidgetForegroundColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#3B82F6";
    public double WidgetOpacity { get; set; } = 0.92;
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 13.0;
    public double WidgetWidth { get; set; } = 250;
    public double WidgetHeight { get; set; } = 250;
    public double WidgetCornerRadius { get; set; } = 14.0;
    public double WidgetLeft { get; set; } = 100;
    public double WidgetTop { get; set; } = 100;
    public WidgetDockPosition DockPosition { get; set; } = WidgetDockPosition.TopRight;
    public WidgetMode WidgetMode { get; set; } = WidgetMode.FloatOnTop;
    public bool WidgetVisible { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public List<int> ReminderIntervalsMinutes { get; set; } = new() { 60, 30, 15 };
    public bool NotificationsEnabled { get; set; } = true;
    public bool NotificationSoundEnabled { get; set; } = true;
    public int DefaultSnoozeDurationMinutes { get; set; } = 15;
    public bool LaunchOnStartup { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool ShowStreakOnWidget { get; set; } = true;
    public int DayEndHour { get; set; } = 0;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
}
