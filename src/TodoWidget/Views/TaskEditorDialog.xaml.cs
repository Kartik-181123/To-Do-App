namespace TodoWidget.Views;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TodoWidget.ViewModels;
public partial class TaskEditorDialog : Window
{
    private readonly TaskEditorViewModel _viewModel;
    private readonly List<TimeSpan> _timeSlots = new();
    public TaskEditorDialog(TaskEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        _viewModel.RequestClose += OnRequestClose;

        PopulateTimeSlots();

        Loaded += TaskEditorDialog_Loaded;
    }

    private void PopulateTimeSlots()
    {
        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += 30)
            {
                var ts = new TimeSpan(hour, minute, 0);
                _timeSlots.Add(ts);

                DateTime dummy = DateTime.Today.Add(ts);
                TimeComboBox.Items.Add(dummy.ToString("h:mm tt"));
            }
        }
        if (_viewModel.DueTime.HasValue)
        {
            var target = _viewModel.DueTime.Value;
            int bestMatchIndex = 0;
            double minDiff = double.MaxValue;

            for (int i = 0; i < _timeSlots.Count; i++)
            {
                double diff = Math.Abs((_timeSlots[i] - target).TotalMinutes);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestMatchIndex = i;
                }
            }

            TimeComboBox.SelectedIndex = bestMatchIndex;
        }
        else
        {
            TimeComboBox.SelectedIndex = 18 * 2;
        }
    }

    private void TaskEditorDialog_Loaded(object sender, RoutedEventArgs e)
    {
        TitleInput.Focus();
        TitleInput.SelectAll();
    }

    private void OnRequestClose(bool success)
    {
        DialogResult = success;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TimeComboBox.SelectedIndex >= 0 && TimeComboBox.SelectedIndex < _timeSlots.Count)
        {
            _viewModel.DueTime = _timeSlots[TimeComboBox.SelectedIndex];
        }
    }
}
