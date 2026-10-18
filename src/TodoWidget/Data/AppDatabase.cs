using System.IO;
using LiteDB;
using TodoWidget.Models;

namespace TodoWidget.Data;
public class AppDatabase : IDisposable
{
    private static readonly Lazy<AppDatabase> _lazyInstance = new(() => new AppDatabase());
    public static AppDatabase Instance => _lazyInstance.Value;

    private readonly LiteDatabase _db;
    private readonly object _syncLock = new();
    private bool _disposed;
    public string DatabasePath { get; }
    public ILiteCollection<TodoTask> Tasks => _db.GetCollection<TodoTask>("tasks");
    public ILiteCollection<StreakData> Streaks => _db.GetCollection<StreakData>("streak");
    public ILiteCollection<AppSettings> Settings => _db.GetCollection<AppSettings>("settings");
    public AppDatabase(string? customDbPath = null)
    {
        if (string.IsNullOrWhiteSpace(customDbPath))
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TodoWidget");

            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            DatabasePath = Path.Combine(appDataDir, "data.db");
        }
        else
        {
            string? directory = Path.GetDirectoryName(customDbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            DatabasePath = customDbPath;
        }

        var mapper = ConfigureMapper();
        var connectionString = new ConnectionString(DatabasePath)
        {
            Connection = ConnectionType.Shared
        };
        _db = new LiteDatabase(connectionString, mapper);

        InitializeDatabase();
    }
    private static BsonMapper ConfigureMapper()
    {
        var mapper = new BsonMapper();
        mapper.RegisterType<HashSet<int>>(
            serialize: set => new BsonArray(set.Select(val => new BsonValue(val))),
            deserialize: bson => bson.IsArray
                ? new HashSet<int>(bson.AsArray.Select(item => item.AsInt32))
                : new HashSet<int>()
        );

        return mapper;
    }
    private void InitializeDatabase()
    {
        lock (_syncLock)
        {
            Tasks.EnsureIndex(t => t.Status);
            Tasks.EnsureIndex(t => t.DueDate);
            Tasks.EnsureIndex(t => t.SortOrder);
            DeduplicateTasks();
        }
    }

    public void DeduplicateTasks()
    {
        lock (_syncLock)
        {
            var all = Tasks.FindAll().ToList();
            if (all.Count <= 1) return;

            // Group by normalized title and rough creation minute to detect duplicated sync tasks
            var groups = all.GroupBy(t => new
            {
                Title = t.Title?.Trim().ToLowerInvariant() ?? string.Empty,
                CreatedMinute = new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day, t.CreatedAt.Hour, t.CreatedAt.Minute, 0)
            }).Where(g => g.Count() > 1);

            foreach (var g in groups)
            {
                // Keep the completed task if any, or the newest updated task
                var keep = g.OrderByDescending(t => t.Status == TodoTaskStatus.Completed ? 1 : 0)
                            .ThenByDescending(t => t.UpdatedAt)
                            .First();

                foreach (var duplicate in g)
                {
                    if (duplicate.Id != keep.Id)
                    {
                        Tasks.Delete(duplicate.Id);
                    }
                }
            }
        }
    }
    public List<TodoTask> GetAllTasks()
    {
        lock (_syncLock)
        {
            return Tasks.FindAll()
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.CreatedAt)
                .ToList();
        }
    }
    public List<TodoTask> GetPendingTasks()
    {
        lock (_syncLock)
        {
            return Tasks.Find(t => t.Status == TodoTaskStatus.Pending)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.EffectiveDueDate)
                .ToList();
        }
    }
    public List<TodoTask> GetTasksDueOnDate(DateTime date)
    {
        lock (_syncLock)
        {
            return Tasks.FindAll()
                .Where(t => t.AppliesToDay(date))
                .OrderBy(t => t.EffectiveDueDate)
                .ToList();
        }
    }
    public bool UpsertTask(TodoTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        bool result;
        lock (_syncLock)
        {
            task.UpdatedAt = DateTime.Now;
            result = Tasks.Upsert(task);
        }

        try
        {
            Services.NotificationService.NotifyTasksChanged();
        }
        catch
        {
        }

        return result;
    }
    public bool DeleteTask(ObjectId id)
    {
        bool result;
        lock (_syncLock)
        {
            result = Tasks.Delete(id);
        }

        try
        {
            Services.NotificationService.NotifyTasksChanged();
        }
        catch
        {
        }

        return result;
    }
    public StreakData GetStreakData()
    {
        lock (_syncLock)
        {
            var data = Streaks.FindById(1);
            if (data == null)
            {
                data = new StreakData { Id = 1 };
                Streaks.Insert(data);
            }
            return data;
        }
    }
    public void SaveStreakData(StreakData streakData)
    {
        ArgumentNullException.ThrowIfNull(streakData);

        lock (_syncLock)
        {
            streakData.Id = 1;
            Streaks.Upsert(streakData);
        }
    }
    public AppSettings GetSettings()
    {
        lock (_syncLock)
        {
            var settings = Settings.FindById(1);
            if (settings == null)
            {
                settings = new AppSettings { Id = 1 };
                Settings.Insert(settings);
            }
            return settings;
        }
    }
    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_syncLock)
        {
            settings.Id = 1;
            Settings.Upsert(settings);
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                lock (_syncLock)
                {
                    _db.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
