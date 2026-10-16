namespace TodoWidget.Views.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TodoWidget.Models;
public partial class TaskItemControl : UserControl
{
    public static readonly DependencyProperty TaskProperty =
        DependencyProperty.Register(
            nameof(Task),
            typeof(TodoTask),
            typeof(TaskItemControl),
            new PropertyMetadata(null));
    public static readonly DependencyProperty CompleteCommandProperty =
        DependencyProperty.Register(
            nameof(CompleteCommand),
            typeof(ICommand),
            typeof(TaskItemControl),
            new PropertyMetadata(null));
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(ICommand),
            typeof(TaskItemControl),
            new PropertyMetadata(null));
    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register(
            nameof(EditCommand),
            typeof(ICommand),
            typeof(TaskItemControl),
            new PropertyMetadata(null));
    public TodoTask? Task
    {
        get => (TodoTask?)GetValue(TaskProperty);
        set => SetValue(TaskProperty, value);
    }
    public ICommand? CompleteCommand
    {
        get => (ICommand?)GetValue(CompleteCommandProperty);
        set => SetValue(CompleteCommandProperty, value);
    }
    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }
    public TaskItemControl()
    {
        InitializeComponent();
    }
}
