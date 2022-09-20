using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Moneo.Functions;

public interface IMoneoTask
{
    string Name { get; }
    string Description { get; }
    bool IsActive { get; }
    Queue<DateTime> Reminders { get; }
    DateTime? DueDate { get; }
    string CompletedMessage { get; }
    string SkippedMessage { get; }
}

public interface IRepeatingTask : IMoneoTask
{
    DateTime? Expiry { get; }
    string RepeatCron { get; }
}

public interface IBadgerTask : IMoneoTask
{
    int BadgerFrequencyMinutes { get; }
    string[] BadgerMessages { get; }
}

public interface IReminder
{
    DateTime DueDate { get; }
}

public class MoneoTask : IMoneoTask
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("description")]
    public string Description { get; set; }
    [JsonProperty("isActive")]
    public bool IsActive { get; set; }
    [JsonProperty("reminders")]
    public Queue<DateTime> Reminders { get; set; }
    [JsonProperty("dueDate")]
    public DateTime? DueDate { get; set; }
    [JsonProperty("completedMessage")]
    public string CompletedMessage { get; set; }
    [JsonProperty("skippedMessage")]
    public string SkippedMessage { get; set; }
    [JsonProperty("repeater")]
    public TaskRepeater Repeater { get; set; }
    [JsonProperty("badger")]
    public TaskBadger Badger { get; set; }
}

public class TaskRepeater
{
    [JsonProperty("expiry")]
    DateTime? Expiry { get; set; }
    [JsonProperty("repeatCron")]
    string RepeatCron { get; set; }
}

public class TaskBadger
{
    [JsonProperty("badgerFrequencyMinutes")]
    int BadgerFrequencyMinutes { get; set; }
    [JsonProperty("badgerMessages")]
    string[] BadgerMessages { get; set; }
}
