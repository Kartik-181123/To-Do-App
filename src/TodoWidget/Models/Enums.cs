namespace TodoWidget.Models;
public enum TodoTaskStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}
public enum WidgetMode
{
    FloatOnTop = 0,
    PinnedToDesktop = 1
}
public enum WidgetDockPosition
{
    Custom = 0,
    TopRight = 1,
    TopLeft = 2,
    BottomRight = 3,
    BottomLeft = 4,
    CenterRight = 5
}
public enum AppTheme
{
    Light = 0,
    Dark = 1,
    System = 2
}
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
