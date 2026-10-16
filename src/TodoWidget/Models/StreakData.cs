using LiteDB;

namespace TodoWidget.Models;
public class StreakData
{
    [BsonId]
    public int Id { get; set; } = 1;
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;
    public DateTime LastStreakDate { get; set; } = DateTime.MinValue;
    public int TotalTasksCompleted { get; set; } = 0;
    public int TotalTasksFailed { get; set; } = 0;
}
