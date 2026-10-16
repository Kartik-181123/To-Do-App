using System.Windows.Input;

namespace TodoWidget.Helpers;
public class RelayCommand : ICommand
{
    private readonly Action<object?>? _execute;
    private readonly Func<object?, Task>? _asyncExecute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;
    private EventHandler? _internalCanExecuteChanged;
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = _ => execute();
        _canExecute = canExecute != null ? _ => canExecute() : null;
    }
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }
    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        _asyncExecute = _ => executeAsync();
        _canExecute = canExecute != null ? _ => canExecute() : null;
    }
    public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        _asyncExecute = executeAsync;
        _canExecute = canExecute;
    }
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _internalCanExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _internalCanExecuteChanged -= value;
        }
    }
    public bool IsExecuting => _isExecuting;
    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        return _canExecute?.Invoke(parameter) ?? true;
    }
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        if (_asyncExecute != null)
        {
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _asyncExecute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
        else
        {
            _execute?.Invoke(parameter);
        }
    }
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
        _internalCanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?>? _execute;
    private readonly Func<T?, Task>? _asyncExecute;
    private readonly Predicate<T?>? _canExecute;
    private bool _isExecuting;
    private EventHandler? _internalCanExecuteChanged;
    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }
    public RelayCommand(Func<T?, Task> executeAsync, Predicate<T?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        _asyncExecute = executeAsync;
        _canExecute = canExecute;
    }
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _internalCanExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _internalCanExecuteChanged -= value;
        }
    }
    public bool IsExecuting => _isExecuting;
    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        if (_canExecute == null)
        {
            return true;
        }

        if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
        {
            return false;
        }

        return _canExecute((T?)parameter);
    }
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        T? typedParam = default;
        if (parameter is T val)
        {
            typedParam = val;
        }

        if (_asyncExecute != null)
        {
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _asyncExecute(typedParam);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
        else
        {
            _execute?.Invoke(typedParam);
        }
    }
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
        _internalCanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
