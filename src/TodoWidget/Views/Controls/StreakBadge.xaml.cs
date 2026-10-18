namespace TodoWidget.Views.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
public partial class StreakBadge : UserControl
{
    public static readonly DependencyProperty StreakCountProperty =
        DependencyProperty.Register(
            nameof(StreakCount),
            typeof(int),
            typeof(StreakBadge),
            new PropertyMetadata(0, OnStreakCountChanged));
    public static readonly DependencyProperty StreakEmojiProperty =
        DependencyProperty.Register(
            nameof(StreakEmoji),
            typeof(string),
            typeof(StreakBadge),
            new PropertyMetadata(""));
    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(
            nameof(DisplayText),
            typeof(string),
            typeof(StreakBadge),
            new PropertyMetadata("0 Days"));
    public int StreakCount
    {
        get => (int)GetValue(StreakCountProperty);
        set => SetValue(StreakCountProperty, value);
    }
    public string StreakEmoji
    {
        get => (string)GetValue(StreakEmojiProperty);
        private set => SetValue(StreakEmojiProperty, value);
    }
    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }
    public StreakBadge()
    {
        InitializeComponent();
        UpdateVisuals(StreakCount, false);
    }

    private static void OnStreakCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StreakBadge badge && e.NewValue is int newCount)
        {
            int oldCount = e.OldValue is int oc ? oc : 0;
            badge.UpdateVisuals(newCount, newCount > oldCount);
        }
    }

    private void UpdateVisuals(int count, bool triggerPulse)
    {
        StreakEmoji = "";
        DisplayText = $"{count} {(count == 1 ? "Day" : "Days")}";

        if (triggerPulse && count > 0)
        {
            bool animationsEnabled = TodoWidget.Data.SettingsManager.Instance?.Current?.EnableAnimations ?? true;
            if (animationsEnabled && Resources["PulseAnimation"] is Storyboard sb)
            {
                sb.Begin();
            }
        }
    }
}
