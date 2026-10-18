using System.Text.Json.Serialization;
using LiteDB;
using TodoWidget.Helpers;

namespace TodoWidget.Models;
public class TodoTask
{
    [BsonId]
    [JsonConverter(typeof(ObjectIdJsonConverter))]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public TodoTaskStatus Status { get; set; } = TodoTaskStatus.Pending;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public HashSet<int> FiredReminderOffsets { get; set; } = new();
    public bool HasBeenSnoozed { get; set; } = false;
    public DateTime? SnoozedUntil { get; set; }
    public int SortOrder { get; set; } = 0;

    public bool IsRecurring { get; set; } = false;
    public List<DayOfWeek> RepeatDays { get; set; } = new();
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public string? VoiceNotePath { get; set; }
    public int VoiceNoteDurationSeconds { get; set; } = 0;

    [BsonIgnore]
    public bool HasVoiceNote => !string.IsNullOrEmpty(VoiceNotePath) && System.IO.File.Exists(VoiceNotePath);

    [BsonIgnore]
    public string VoiceNoteDurationText => VoiceNoteDurationSeconds > 0
        ? $"{VoiceNoteDurationSeconds / 60:D2}:{VoiceNoteDurationSeconds % 60:D2}"
        : "00:00";

    public bool AppliesToDay(DateTime date)
    {
        if (!IsRecurring)
        {
            return DueDate.HasValue && DueDate.Value.Date == date.Date;
        }
        return RepeatDays.Contains(date.DayOfWeek);
    }

    [BsonIgnore]
    public string RecurringDaysSummary
    {
        get
        {
            if (!IsRecurring || RepeatDays == null || RepeatDays.Count == 0) return string.Empty;
            var dayNames = RepeatDays.OrderBy(d => (int)d).Select(d => d.ToString().Substring(0, 3).ToUpper());
            string daysStr = string.Join(", ", dayNames);
            if (StartTime.HasValue && EndTime.HasValue)
            {
                var s = DateTime.Today.Add(StartTime.Value).ToString("h:mm tt");
                var e = DateTime.Today.Add(EndTime.Value).ToString("h:mm tt");
                return $"{daysStr}: {s} - {e}";
            }
            if (StartTime.HasValue)
            {
                var s = DateTime.Today.Add(StartTime.Value).ToString("h:mm tt");
                return $"{daysStr}: {s}";
            }
            return daysStr;
        }
    }

    [BsonIgnore]
    public DateTime? EffectiveDueDate
    {
        get
        {
            if (SnoozedUntil.HasValue) return SnoozedUntil.Value;
            if (DueDate.HasValue) return DueDate.Value;
            if (IsRecurring && StartTime.HasValue)
            {
                return DateTime.Today.Add(StartTime.Value);
            }
            return null;
        }
    }
    [BsonIgnore]
    public bool IsOverdue =>
        Status == TodoTaskStatus.Pending &&
        EffectiveDueDate.HasValue &&
        DateTime.Now > EffectiveDueDate.Value;
    [BsonIgnore]
    public bool CanSnooze =>
        DueDate.HasValue &&
        !HasBeenSnoozed &&
        Status == TodoTaskStatus.Pending;
}
