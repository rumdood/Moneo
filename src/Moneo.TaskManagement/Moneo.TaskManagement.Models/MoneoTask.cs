using Moneo.Core;
using Newtonsoft.Json;

namespace Moneo.TaskManagement.Models;

public interface IMoneoTask
{
    string Id { get; }
    long ConversationId { get; }
    string Name { get; }
    string? Description { get; }
    bool IsActive { get; }
    HashSet<DateTime> DueDates { get; }
    string TimeZone { get; }
    string? CompletedMessage { get; }
    string? SkippedMessage { get; }
    TaskRepeater? Repeater { get; }
    TaskBadger? Badger { get; }
    DateTime Created { get; }
    DateTime LastUpdated { get; }
}

public interface IMoneoTaskState : IMoneoTask
{
    Dictionary<long, TaskReminder> Reminders { get; }
    FixedLengthList<DateTime?> CompletedHistory { get; }
    FixedLengthList<DateTime?> SkippedHistory { get; }
    HashSet<DateTime> ScheduledChecks { get; }
}

public interface IMoneoTaskDto : IMoneoTask
{
    DateTimeOffset[] Reminders { get; set; }
    IEnumerable<DateTime?> CompletedHistory { get; set; }
    IEnumerable<DateTime?> SkippedHistory { get; set; }
}

public abstract class MoneoTask : IMoneoTask
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("conversationId")]
    public long ConversationId { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("description")]
    public string? Description { get; set; }
    [JsonProperty("isActive")]
    public bool IsActive { get; set; }
    [JsonProperty("dueDates")]
    public HashSet<DateTime> DueDates { get; set; } = new ();
    [JsonProperty("timeZone")]
    public string TimeZone { get; set; } = "";
    [JsonProperty("completedMessage")]
    public string? CompletedMessage { get; set; }
    [JsonProperty("skippedMessage")]
    public string? SkippedMessage { get; set; }
    [JsonProperty("repeater")]
    public TaskRepeater? Repeater { get; set; }
    [JsonProperty("badger")]
    public TaskBadger? Badger { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class MoneoTaskState : MoneoTask, IMoneoTaskState
{
    [JsonProperty("reminders")]
    public Dictionary<long, TaskReminder> Reminders { get; set; } = new();
    [JsonProperty("lastCompletedOn")]
    public FixedLengthList<DateTime?> CompletedHistory { get; set; } = new(5);
    [JsonProperty("lastSkippedOn")]
    public FixedLengthList<DateTime?> SkippedHistory { get; set; } = new(5);
    [JsonProperty("scheduledChecks")]
    public HashSet<DateTime> ScheduledChecks { get; set; } = new();

    public void Deconstruct(
        out bool isActive, 
        out FixedLengthList<DateTime?> lastCompletedOn, 
        out FixedLengthList<DateTime?> lastSkippedOn, 
        out TaskRepeater? repeater, 
        out TaskBadger? badger,
        out HashSet<DateTime> scheduledChecks)
    {
        isActive = IsActive;
        lastCompletedOn = CompletedHistory;
        lastSkippedOn = SkippedHistory;
        repeater = Repeater;
        badger = Badger;
        scheduledChecks = ScheduledChecks;
    }
}

public class MoneoTaskDto : MoneoTask, IMoneoTaskDto
{
    [JsonProperty("reminders")]
    public DateTimeOffset[] Reminders { get; set; } = Array.Empty<DateTimeOffset>();
    [JsonProperty("completedHistory")]
    public IEnumerable<DateTime?> CompletedHistory { get; set; } = Enumerable.Empty<DateTime?>();
    [JsonProperty("skippedHistory")]
    public IEnumerable<DateTime?> SkippedHistory { get; set; } = Enumerable.Empty<DateTime?>();
}
