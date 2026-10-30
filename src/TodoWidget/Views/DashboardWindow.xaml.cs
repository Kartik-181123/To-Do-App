namespace TodoWidget.Views;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TodoWidget.Data;
using TodoWidget.Models;
using TodoWidget.Services;
using TodoWidget.ViewModels;

public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly AppDatabase _database;
    private bool _isExplicitExit;

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

    public void ForceClose()
    {
        _isExplicitExit = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
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
                    if (NavWorkbenchRadio != null) NavWorkbenchRadio.IsChecked = true;
                    break;
                case 1:
                    if (NavFocusRadio != null) NavFocusRadio.IsChecked = true;
                    break;
                case 2:
                    if (NavBlockerRadio != null) NavBlockerRadio.IsChecked = true;
                    break;
                case 3:
                    if (NavAnalyticsRadio != null) NavAnalyticsRadio.IsChecked = true;
                    break;
                case 4:
                    if (NavAccountRadio != null) NavAccountRadio.IsChecked = true;
                    break;
                case 5:
                    if (NavSettingsRadio != null) NavSettingsRadio.IsChecked = true;
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
                From = 0.45,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(160),
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

    private void HideAllTabs()
    {
        if (WorkbenchTabView != null) WorkbenchTabView.Visibility = Visibility.Collapsed;
        if (FocusRoomTabView != null) FocusRoomTabView.Visibility = Visibility.Collapsed;
        if (BlockerTabView != null) BlockerTabView.Visibility = Visibility.Collapsed;
        if (AnalyticsTabView != null) AnalyticsTabView.Visibility = Visibility.Collapsed;
        if (AccountTabView != null) AccountTabView.Visibility = Visibility.Collapsed;
        if (SettingsTabView != null) SettingsTabView.Visibility = Visibility.Collapsed;
    }

    private void NavWorkbench_Checked(object sender, RoutedEventArgs e)
    {
        if (WorkbenchTabView == null) return;
        HideAllTabs();
        WorkbenchTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(WorkbenchTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 0;
            _viewModel.RefreshAll();
        }
    }

    private void NavFocus_Checked(object sender, RoutedEventArgs e)
    {
        if (FocusRoomTabView == null) return;
        HideAllTabs();
        FocusRoomTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(FocusRoomTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 1;
            _viewModel.RefreshAll();
        }
    }

    private void NavBlocker_Checked(object sender, RoutedEventArgs e)
    {
        if (BlockerTabView == null) return;
        HideAllTabs();
        BlockerTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(BlockerTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 2;
            _viewModel.RefreshAll();
        }
    }

    private void NavAnalytics_Checked(object sender, RoutedEventArgs e)
    {
        if (AnalyticsTabView == null) return;
        HideAllTabs();
        AnalyticsTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(AnalyticsTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 3;
            _viewModel.RefreshAll();
        }
    }

    private void NavAccount_Checked(object sender, RoutedEventArgs e)
    {
        if (AccountTabView == null) return;
        HideAllTabs();
        AccountTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(AccountTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 4;
            _viewModel.CloudSync.LoadFromSettings();
        }
    }

    private void NavSettings_Checked(object sender, RoutedEventArgs e)
    {
        if (SettingsTabView == null) return;
        HideAllTabs();
        SettingsTabView.Visibility = Visibility.Visible;
        AnimateTabTransition(SettingsTabView);

        if (_viewModel != null)
        {
            _viewModel.SelectedTabIndex = 5;
        }
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    _viewModel.AddCustomAppFromFileCommand.Execute(file);
                }
            }
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void BrowseApp_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application to Block",
            Filter = "Applications & Shortcuts (*.exe;*.lnk)|*.exe;*.lnk|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (var filename in openFileDialog.FileNames)
            {
                _viewModel.AddCustomAppFromFileCommand.Execute(filename);
            }
        }
    }

    private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is TodoTask task)
        {
            _viewModel.SelectedTask = task;
        }
    }

    private void TaskRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedTask != null)
        {
            _viewModel.EditTaskCommand.Execute(_viewModel.SelectedTask);
        }
    }

    private void QuickAddTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.QuickAddTaskCommand.Execute(null);
        }
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
}