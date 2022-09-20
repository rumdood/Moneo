﻿using Newtonsoft.Json;

namespace Moneo.Models;

public interface IMoneoTask
{
    string Name { get; }
    string? Description { get; }
    bool IsActive { get; }
    DateTime? CompletedOn { get; }
    DateTime? SkippedOn { get; }
    HashSet<DateTime> DueDates { get; }
    string TimeZone { get; }
    string? CompletedMessage { get; }
    string? SkippedMessage { get; }
    TaskRepeater? Repeater { get; }
    TaskBadger? Badger { get; }
}

public interface IMoneoTaskWithReminders : IMoneoTask
{
    Dictionary<long, TaskReminder> Reminders { get; }
}

public interface IMoneoTaskDto : IMoneoTask
{
    DateTimeOffset[] Reminders { get; set; }
}

public abstract class MoneoTask : IMoneoTask
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("description")]
    public string? Description { get; set; }
    [JsonProperty("isActive")]
    public bool IsActive { get; set; }
    [JsonProperty("completedOn")]
    public DateTime? CompletedOn { get; set; }
    [JsonProperty("skippedOn")]
    public DateTime? SkippedOn { get; set; }
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
}

public class MoneoTaskWithReminders : MoneoTask, IMoneoTaskWithReminders
{
    [JsonProperty("reminders")]
    public Dictionary<long, TaskReminder> Reminders { get; set; } = new Dictionary<long, TaskReminder>();
}

public class MoneoTaskDto : MoneoTask, IMoneoTaskDto
{
    [JsonProperty("reminders")]
    public DateTimeOffset[] Reminders { get; set; } = new DateTimeOffset[0];
}
