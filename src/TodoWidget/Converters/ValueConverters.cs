namespace TodoWidget.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TodoWidget.Helpers;
using TodoWidget.Models;
public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolVal = value is bool b && b;
        if (Invert)
        {
            boolVal = !boolVal;
        }

        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility vis)
        {
            bool isVisible = vis == Visibility.Visible;
            return Invert ? !isVisible : isVisible;
        }

        return false;
    }
}
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}
public class PriorityToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush LowBrush = new(Color.FromRgb(16, 185, 129));
    private static readonly SolidColorBrush MediumBrush = new(Color.FromRgb(245, 158, 11));
    private static readonly SolidColorBrush HighBrush = new(Color.FromRgb(249, 115, 22));
    private static readonly SolidColorBrush CriticalBrush = new(Color.FromRgb(239, 68, 68));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(148, 163, 184));

    static PriorityToBrushConverter()
    {
        LowBrush.Freeze();
        MediumBrush.Freeze();
        HighBrush.Freeze();
        CriticalBrush.Freeze();
        DefaultBrush.Freeze();
    }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => LowBrush,
                TaskPriority.Medium => MediumBrush,
                TaskPriority.High => HighBrush,
                TaskPriority.Critical => CriticalBrush,
                _ => DefaultBrush
            };
        }

        return DefaultBrush;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(16, 185, 129));
    private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(239, 68, 68));
    private static readonly SolidColorBrush PendingBrush = new(Color.FromRgb(148, 163, 184));

    static StatusToBrushConverter()
    {
        CompletedBrush.Freeze();
        FailedBrush.Freeze();
        PendingBrush.Freeze();
    }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TodoTaskStatus status)
        {
            return status switch
            {
                TodoTaskStatus.Completed => CompletedBrush,
                TodoTaskStatus.Failed => FailedBrush,
                _ => PendingBrush
            };
        }

        return PendingBrush;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class StatusToStrikethroughConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TodoTaskStatus status && status == TodoTaskStatus.Completed)
        {
            return TextDecorations.Strikethrough;
        }

        if (value is bool isCompleted && isCompleted)
        {
            return TextDecorations.Strikethrough;
        }

        return null;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class StatusToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TodoTaskStatus status && status == TodoTaskStatus.Completed)
        {
            return 0.5;
        }

        if (value is bool isCompleted && isCompleted)
        {
            return 0.5;
        }

        return 1.0;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class DueDateFormatterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
        {
            return string.Empty;
        }

        DateTime localDate = dt.ToLocalTime();
        DateTime today = DateTime.Today;

        if (localDate.Date == today)
        {
            return $"Today, {localDate:h:mm tt}";
        }

        if (localDate.Date == today.AddDays(1))
        {
            return $"Tomorrow, {localDate:h:mm tt}";
        }

        if (localDate.Date == today.AddDays(-1))
        {
            return $"Yesterday, {localDate:h:mm tt}";
        }

        return localDate.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class IntToStreakTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int streak)
        {
            return streak > 0
                ? $"🔥 {streak} {(streak == 1 ? "Day" : "Days")}"
                : "💔 0 Days";
        }

        return "💔 0 Days";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return ColorHelper.BrushFromHex(hex);
            }
            catch
            {
            }
        }

        return Brushes.Transparent;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
        }

        return "#00000000";
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasText = value is string s && !string.IsNullOrWhiteSpace(s);
        if (Invert) hasText = !hasText;
        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
