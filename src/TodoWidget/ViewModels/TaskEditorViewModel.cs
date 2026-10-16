namespace TodoWidget.ViewModels;

using System;
using System.Collections.Generic;
using TodoWidget.Data;
using TodoWidget.Helpers;
using TodoWidget.Models;
public class TaskEditorViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly TodoTask? _existingTask;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private DateTime? _dueDate = DateTime.Today;
    private TimeSpan? _dueTime = new TimeSpan(18, 0, 0);
    private TaskPriority _priority = TaskPriority.Medium;
    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }
    }
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }
    public TimeSpan? DueTime
    {
        get => _dueTime;
        set => SetProperty(ref _dueTime, value);
    }
    public TaskPriority Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    private bool _isRecurring;
    private bool _repeatMon;
    private bool _repeatTue;
    private bool _repeatWed;
    private bool _repeatThu;
    private bool _repeatFri;
    private bool _repeatSat;
    private bool _repeatSun;
    private TimeSpan? _startTime = new TimeSpan(9, 0, 0);
    private TimeSpan? _endTime = new TimeSpan(11, 0, 0);

    public event Action<bool>? RequestClose;
    public IEnumerable<TaskPriority> AvailablePriorities => Enum.GetValues<TaskPriority>();

    public bool IsRecurring
    {
        get => _isRecurring;
        set => SetProperty(ref _isRecurring, value);
    }

    public bool RepeatMon
    {
        get => _repeatMon;
        set => SetProperty(ref _repeatMon, value);
    }

    public bool RepeatTue
    {
        get => _repeatTue;
        set => SetProperty(ref _repeatTue, value);
    }

    public bool RepeatWed
    {
        get => _repeatWed;
        set => SetProperty(ref _repeatWed, value);
    }

    public bool RepeatThu
    {
        get => _repeatThu;
        set => SetProperty(ref _repeatThu, value);
    }

    public bool RepeatFri
    {
        get => _repeatFri;
        set => SetProperty(ref _repeatFri, value);
    }

    public bool RepeatSat
    {
        get => _repeatSat;
        set => SetProperty(ref _repeatSat, value);
    }

    public bool RepeatSun
    {
        get => _repeatSun;
        set => SetProperty(ref _repeatSun, value);
    }

    public TimeSpan? StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public TimeSpan? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        private set => SetProperty(ref _isEditing, value);
    }

    public string DialogTitle => IsEditing ? "Edit Task" : "New Task";
    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public TaskEditorViewModel(AppDatabase database, TodoTask? task = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _existingTask = task;

        SaveCommand = new RelayCommand(ExecuteSave);
        CancelCommand = new RelayCommand(ExecuteCancel);

        if (task != null)
        {
            IsEditing = true;
            Title = task.Title ?? string.Empty;
            Description = task.Description ?? string.Empty;
            Priority = task.Priority;

            IsRecurring = task.IsRecurring;
            if (task.RepeatDays != null)
            {
                RepeatMon = task.RepeatDays.Contains(DayOfWeek.Monday);
                RepeatTue = task.RepeatDays.Contains(DayOfWeek.Tuesday);
                RepeatWed = task.RepeatDays.Contains(DayOfWeek.Wednesday);
                RepeatThu = task.RepeatDays.Contains(DayOfWeek.Thursday);
                RepeatFri = task.RepeatDays.Contains(DayOfWeek.Friday);
                RepeatSat = task.RepeatDays.Contains(DayOfWeek.Saturday);
                RepeatSun = task.RepeatDays.Contains(DayOfWeek.Sunday);
            }

            StartTime = task.StartTime ?? new TimeSpan(9, 0, 0);
            EndTime = task.EndTime ?? new TimeSpan(11, 0, 0);

            if (task.DueDate.HasValue)
            {
                DueDate = task.DueDate.Value.Date;
                DueTime = task.DueDate.Value.TimeOfDay;
            }
            else
            {
                DueDate = null;
                DueTime = null;
            }
        }
        else
        {
            IsEditing = false;
            DueDate = DateTime.Today;
            DueTime = new TimeSpan(18, 0, 0);
            Priority = TaskPriority.Medium;
            IsRecurring = false;
            StartTime = new TimeSpan(9, 0, 0);
            EndTime = new TimeSpan(11, 0, 0);
        }
    }

    private void ExecuteSave()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }

        var repeatDaysList = new List<DayOfWeek>();
        if (IsRecurring)
        {
            if (RepeatMon) repeatDaysList.Add(DayOfWeek.Monday);
            if (RepeatTue) repeatDaysList.Add(DayOfWeek.Tuesday);
            if (RepeatWed) repeatDaysList.Add(DayOfWeek.Wednesday);
            if (RepeatThu) repeatDaysList.Add(DayOfWeek.Thursday);
            if (RepeatFri) repeatDaysList.Add(DayOfWeek.Friday);
            if (RepeatSat) repeatDaysList.Add(DayOfWeek.Saturday);
            if (RepeatSun) repeatDaysList.Add(DayOfWeek.Sunday);

            if (repeatDaysList.Count == 0)
            {
                ErrorMessage = "Please select at least one day for the recurring task.";
                return;
            }
        }

        DateTime? finalDueDate = null;
        if (!IsRecurring && DueDate.HasValue)
        {
            var date = DueDate.Value.Date;
            var time = DueTime ?? new TimeSpan(23, 59, 0);
            finalDueDate = date.Add(time);
        }

        if (IsEditing && _existingTask != null)
        {
            _existingTask.Title = Title.Trim();
            _existingTask.Description = Description?.Trim() ?? string.Empty;
            _existingTask.DueDate = finalDueDate;
            _existingTask.Priority = Priority;
            _existingTask.IsRecurring = IsRecurring;
            _existingTask.RepeatDays = repeatDaysList;
            _existingTask.StartTime = IsRecurring ? StartTime : null;
            _existingTask.EndTime = IsRecurring ? EndTime : null;
            _database.UpsertTask(_existingTask);
        }
        else
        {
            var newTask = new TodoTask
            {
                Title = Title.Trim(),
                Description = Description?.Trim() ?? string.Empty,
                DueDate = finalDueDate,
                Priority = Priority,
                Status = TodoTaskStatus.Pending,
                CreatedAt = DateTime.Now,
                IsRecurring = IsRecurring,
                RepeatDays = repeatDaysList,
                StartTime = IsRecurring ? StartTime : null,
                EndTime = IsRecurring ? EndTime : null
            };
            _database.UpsertTask(newTask);
        }

        RequestClose?.Invoke(true);
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(false);
    }
}
