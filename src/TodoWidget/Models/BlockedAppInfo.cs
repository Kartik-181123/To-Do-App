namespace TodoWidget.Models;

using System;
using System.Text.Json.Serialization;
using System.Windows.Media;
using LiteDB;

/// <summary>
/// Represents an application configured for active blocking during Focus Mode sessions.
/// </summary>
public class BlockedAppInfo
{
    public string Name { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsCustom { get; set; } = false;
    public DateTime AddedAt { get; set; } = DateTime.Now;

    [BsonIgnore]
    [JsonIgnore]
    public ImageSource? Icon { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    public string DisplayProcessName =>
        string.IsNullOrWhiteSpace(ProcessName)
            ? string.Empty
            : (ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? ProcessName : $"{ProcessName}.exe");
}
