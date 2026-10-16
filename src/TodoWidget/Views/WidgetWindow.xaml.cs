namespace TodoWidget.Views;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Interop;
using TodoWidget.Models;
using TodoWidget.ViewModels;
public partial class WidgetWindow : Window
{
    private readonly WidgetViewModel _viewModel;
    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isLoaded;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    public WidgetWindow(WidgetViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        Loaded += WidgetWindow_Loaded;
        LocationChanged += WidgetWindow_LocationChanged;
        SizeChanged += WidgetWindow_SizeChanged;

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SettingsChanged += OnSettingsChanged;
        }
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        try
        {
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
        }
        catch
        {
        }

        ApplyWidgetMode();
    }

    private void WidgetWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        RestorePositionAndAppearance();
    }

    private void WidgetWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (!_isLoaded || WindowState == WindowState.Minimized) return;

        var settings = SettingsManager.Instance?.Current;
        if (settings != null)
        {
            settings.WidgetLeft = Left;
            settings.WidgetTop = Top;
            settings.DockPosition = WidgetDockPosition.Custom;
            SettingsManager.Instance?.Save();
        }
    }

    private void WidgetWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isLoaded || WindowState == WindowState.Minimized) return;

        var settings = SettingsManager.Instance?.Current;
        if (settings != null)
        {
            settings.WidgetWidth = Width;
            settings.WidgetHeight = Height;
            SettingsManager.Instance?.Save();
        }
    }

    private void RestorePositionAndAppearance()
    {
        var settings = SettingsManager.Instance?.Current;
        if (settings == null) return;
        if (settings.WidgetWidth >= 160)
        {
            Width = settings.WidgetWidth;
        }

        if (settings.WidgetHeight >= 160)
        {
            Height = settings.WidgetHeight;
        }

        var workArea = SystemParameters.WorkArea;

        switch (settings.DockPosition)
        {
            case WidgetDockPosition.TopRight:
                Left = workArea.Right - Width - 24;
                Top = workArea.Top + 24;
                break;
            case WidgetDockPosition.TopLeft:
                Left = workArea.Left + 24;
                Top = workArea.Top + 24;
                break;
            case WidgetDockPosition.BottomRight:
                Left = workArea.Right - Width - 24;
                Top = workArea.Bottom - Height - 24;
                break;
            case WidgetDockPosition.BottomLeft:
                Left = workArea.Left + 24;
                Top = workArea.Bottom - Height - 24;
                break;
            case WidgetDockPosition.CenterRight:
                Left = workArea.Right - Width - 24;
                Top = workArea.Top + (workArea.Height - Height) / 2;
                break;
            default:
                double virtualLeft = SystemParameters.VirtualScreenLeft;
                double virtualTop = SystemParameters.VirtualScreenTop;
                double virtualWidth = SystemParameters.VirtualScreenWidth;
                double virtualHeight = SystemParameters.VirtualScreenHeight;

                if (settings.WidgetLeft >= virtualLeft && settings.WidgetLeft + Width <= virtualLeft + virtualWidth)
                {
                    Left = settings.WidgetLeft;
                }
                else
                {
                    Left = workArea.Right - Width - 24;
                }

                if (settings.WidgetTop >= virtualTop && settings.WidgetTop + Height <= virtualTop + virtualHeight)
                {
                    Top = settings.WidgetTop;
                }
                else
                {
                    Top = workArea.Top + 24;
                }
                break;
        }

        ApplyAppearanceSettings(settings);
    }

    private void ApplyAppearanceSettings(AppSettings settings)
    {
        if (settings == null) return;

        try
        {
            if (!string.IsNullOrWhiteSpace(settings.WidgetBackgroundColor))
            {
                var baseColor = ColorHelper.FromHex(settings.WidgetBackgroundColor);
                byte alpha = (byte)(Math.Clamp(settings.WidgetOpacity, 0.1, 1.0) * 255);
                var brush = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
                MainWidgetBorder.Background = brush;
            }

            if (settings.WidgetCornerRadius >= 0)
            {
                MainWidgetBorder.CornerRadius = new CornerRadius(settings.WidgetCornerRadius);
                HeaderBar.CornerRadius = new CornerRadius(
                    Math.Max(0, settings.WidgetCornerRadius - 1),
                    Math.Max(0, settings.WidgetCornerRadius - 1),
                    0,
                    0);
            }

            if (!string.IsNullOrWhiteSpace(settings.FontFamily))
            {
                FontFamily = new FontFamily(settings.FontFamily);
            }

            if (settings.FontSize > 0)
            {
                FontSize = settings.FontSize;
            }
        }
        catch
        {
        }

        ApplyWidgetMode();
    }

    private void ApplyWidgetMode()
    {
        if (_hwnd == IntPtr.Zero) return;

        var settings = SettingsManager.Instance?.Current;
        var mode = settings?.WidgetMode ?? WidgetMode.FloatOnTop;

        try
        {
            if (mode == WidgetMode.FloatOnTop)
            {
                DesktopPinner.UnpinFromDesktop(_hwnd);
                DesktopPinner.SetAlwaysOnTop(_hwnd, true);
                Topmost = true;
            }
            else
            {
                Topmost = false;
                DesktopPinner.SetAlwaysOnTop(_hwnd, false);
                DesktopPinner.PinToDesktop(_hwnd);
            }
        }
        catch
        {
            Topmost = mode == WidgetMode.FloatOnTop;
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        Dispatcher.Invoke(() =>
        {
            var settings = e ?? SettingsManager.Instance?.Current;
            if (settings != null)
            {
                RestorePositionAndAppearance();
                Visibility = settings.WidgetVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        });
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsManager.Instance?.Current;
        if (settings != null)
        {
            settings.WidgetVisible = false;
            SettingsManager.Instance?.Save();
        }

        Hide();
    }

    private void QuickAddInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_viewModel.AddQuickTaskCommand.CanExecute(null))
            {
                _viewModel.AddQuickTaskCommand.Execute(null);
            }
        }
    }
}
