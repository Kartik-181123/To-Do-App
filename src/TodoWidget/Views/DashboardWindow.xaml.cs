namespace TodoWidget.Views;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TodoWidget.Data;
using TodoWidget.Models;
using TodoWidget.ViewModels;
public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly AppDatabase _database;
    public DashboardWindow(DashboardViewModel viewModel, AppDatabase database)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        DataContext = _viewModel;

        _viewModel.RequestAddTask += OnRequestAddTask;
        _viewModel.RequestEditTask += OnRequestEditTask;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        TrySetWindowIcon();
    }

    private void TrySetWindowIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "taskbar-icon.png");
            if (File.Exists(iconPath))
            {
                Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
            }
        }
        catch
        {
        }
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void OnRequestAddTask()
    {
        var editorVm = new TaskEditorViewModel(_database);
        var dialog = new TaskEditorDialog(editorVm)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.RefreshAll();
        }
    }

    private void OnRequestEditTask(TodoTask task)
    {
        if (task == null) return;

        var editorVm = new TaskEditorViewModel(_database, task);
        var dialog = new TaskEditorDialog(editorVm)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.RefreshAll();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.SelectedTabIndex))
        {
            switch (_viewModel.SelectedTabIndex)
            {
                case 0:
                    NavTasksRadio.IsChecked = true;
                    break;
                case 1:
                    NavStatsRadio.IsChecked = true;
                    break;
                case 2:
                    NavAccountRadio.IsChecked = true;
                    break;
                case 3:
                    NavSettingsRadio.IsChecked = true;
                    break;
            }
        }
    }

    private void AnimateTabTransition(FrameworkElement activeTab)
    {
        bool animations = SettingsManager.Instance?.Current?.EnableAnimations ?? true;
        if (animations)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.4,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            activeTab.BeginAnimation(UIElement.OpacityProperty, anim);
        }
        else
        {
            activeTab.BeginAnimation(UIElement.OpacityProperty, null);
            activeTab.Opacity = 1.0;
        }
    }

    private void NavTasks_Checked(object sender, RoutedEventArgs e)
    {
        if (TasksTabView == null) return;

        TasksTabView.Visibility = Visibility.Visible;
        StatsTabView.Visibility = Visibility.Collapsed;
        AccountTabView.Visibility = Visibility.Collapsed;
        SettingsTabView.Visibility = Visibility.Collapsed;
        AnimateTabTransition(TasksTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 0;
            _viewModel.RefreshAll();
        }
    }

    private void NavStats_Checked(object sender, RoutedEventArgs e)
    {
        if (StatsTabView == null) return;

        TasksTabView.Visibility = Visibility.Collapsed;
        StatsTabView.Visibility = Visibility.Visible;
        AccountTabView.Visibility = Visibility.Collapsed;
        SettingsTabView.Visibility = Visibility.Collapsed;
        AnimateTabTransition(StatsTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 1;
            _viewModel.RefreshAll();
        }
    }

    private void NavAccount_Checked(object sender, RoutedEventArgs e)
    {
        if (AccountTabView == null) return;

        TasksTabView.Visibility = Visibility.Collapsed;
        StatsTabView.Visibility = Visibility.Collapsed;
        AccountTabView.Visibility = Visibility.Visible;
        SettingsTabView.Visibility = Visibility.Collapsed;
        AnimateTabTransition(AccountTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 2;
            _viewModel.CloudSync.LoadFromSettings();
        }
    }

    private void NavSettings_Checked(object sender, RoutedEventArgs e)
    {
        if (SettingsTabView == null) return;

        TasksTabView.Visibility = Visibility.Collapsed;
        StatsTabView.Visibility = Visibility.Collapsed;
        AccountTabView.Visibility = Visibility.Collapsed;
        SettingsTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(SettingsTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 3;
        }
    }

    private void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || StatusFilterCombo == null) return;

        _viewModel.StatusFilter = StatusFilterCombo.SelectedIndex switch
        {
            1 => TodoTaskStatus.Pending,
            2 => TodoTaskStatus.Completed,
            3 => TodoTaskStatus.Failed,
            _ => null
        };
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch
        {
        }
    }
}
