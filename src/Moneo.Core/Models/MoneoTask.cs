﻿using Moneo.Core;
using Newtonsoft.Json;

namespace Moneo.Models;

public interface IMoneoTask
{
    string Name { get; }
    string? Description { get; }
    bool IsActive { get; }
    HashSet<DateTime> DueDates { get; }
    string TimeZone { get; }
    DateTime? LastCompletedOn { get; }
    string? CompletedMessage { get; }
    DateTime? LastSkippedOn { get; }
    string? SkippedMessage { get; }
    TaskRepeater? Repeater { get; }
    TaskBadger? Badger { get; }
    DateTime Created { get; }
    DateTime LastUpdated { get; }
}

public interface IMoneoTaskState : IMoneoTask
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
    [JsonProperty("dueDates")]
    public HashSet<DateTime> DueDates { get; set; } = new ();
    [JsonProperty("timeZone")]
    public string TimeZone { get; set; } = "";
    [JsonProperty("lastCompletedOn")]
    public DateTime? LastCompletedOn { get; set; }
    [JsonProperty("completedMessage")]
    public string? CompletedMessage { get; set; }
    [JsonProperty("lastSkippedOn")]
    public DateTime? LastSkippedOn { get; set; }
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
    public const int MaxDateHistory = 5;

    [JsonProperty("reminders")]
    public Dictionary<long, TaskReminder> Reminders { get; set; } = new();

    public void Deconstruct(
        out bool isActive, 
        out DateTime? lastCompletedOn, 
        out DateTime? lastSkippedOn, 
        out TaskRepeater? repeater, 
        out TaskBadger? badger)
    {
        isActive = IsActive;
        lastCompletedOn = LastCompletedOn;
        lastSkippedOn = LastSkippedOn;
        repeater = Repeater;
        badger = Badger;
    }
}

public class MoneoTaskDto : MoneoTask, IMoneoTaskDto
{
    [JsonProperty("reminders")]
    public DateTimeOffset[] Reminders { get; set; } = Array.Empty<DateTimeOffset>();

    public DateTime? LastCompletedOn { get; init; }
    public DateTime? LastSkippedOn { get; init; }
}
