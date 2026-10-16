using TodoWidget.Models;

namespace TodoWidget.Data;
public sealed class SettingsManager
{
    private static readonly Lazy<SettingsManager> _lazyInstance = new(() => new SettingsManager());
    private readonly object _lock = new();
    private AppDatabase _database;
    private AppSettings _current;
    public static SettingsManager Instance => _lazyInstance.Value;
    public AppSettings Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
        private set
        {
            lock (_lock)
            {
                _current = value;
            }
        }
    }
    public event EventHandler<AppSettings>? SettingsChanged;
    private SettingsManager()
    {
        _database = AppDatabase.Instance;
        _current = _database.GetSettings();
    }
    public void SetDatabase(AppDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        lock (_lock)
        {
            _database = database;
            _current = _database.GetSettings();
        }
    }
    public AppSettings Load()
    {
        lock (_lock)
        {
            _current = _database.GetSettings();
            return _current;
        }
    }
    public void Save(AppSettings? settings = null)
    {
        AppSettings toSave;
        lock (_lock)
        {
            if (settings != null)
            {
                _current = settings;
            }

            toSave = _current;
            _database.SaveSettings(toSave);
        }

        SettingsChanged?.Invoke(this, toSave);
    }
}
