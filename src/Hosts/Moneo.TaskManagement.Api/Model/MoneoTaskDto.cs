namespace Moneo.TaskManagement.Model;

public enum MoneoTaskEventType
{
    Completed,
    Skipped,
    Deactivated,
}

public record MoneoTaskDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> CompletedMessages { get; set; }
    public bool CanBeSkipped { get; set; }
    public IReadOnlyList<string> SkippedMessages { get; set; }
    public string Timezone { get; set; } = "";
    public DateTime? DueOn { get; set; }
    public int? BadgerFrequencyInMinutes { get; set; }
    public string? BadgerMessages { get; set; }
    public TaskRepeaterDto? Repeater { get; set; }
}

public record MoneoTaskHistoryRecordDto
{
    public DateTime Timestamp { get; init; }
    public MoneoTaskEventType EventType { get; init; }
}

public record MoneoTaskWithHistoryDto : MoneoTaskDto
{
    public IReadOnlyList<MoneoTaskHistoryRecordDto> History { get; set; }
}

public record TaskRepeaterDto
{
    public string RepeatCron { get; }
    public DateTime? Expiry { get; }
    public int? EarlyCompletionThresholdHours { get; }

    public TaskRepeaterDto(string repeatCron, DateTime? expiry, int? earlyCompletionThresholdHours)
    {
        if (string.IsNullOrEmpty(repeatCron))
        {
            throw new ArgumentException("RepeatCron cannot be null or empty", nameof(repeatCron));
        }

        RepeatCron = repeatCron;
        Expiry = expiry;
        EarlyCompletionThresholdHours = earlyCompletionThresholdHours;
    }
}

public record CreateTaskDto(
    string Name,
    string Description,
    bool IsActive,
    List<string> CompletedMessages,
    bool CanBeSkipped,
    List<string> SkippedMessages,
    string Timezone,
    DateTime? DueOn,
    int? BadgerFrequencyInMinutes,
    string? BadgerMessages,
    TaskRepeaterDto? Repeater
);

public record UpdateTaskDto(
    string Name,
    string Description,
    bool IsActive,
    List<string> CompletedMessages,
    bool CanBeSkipped,
    List<string> SkippedMessages,
    string Timezone,
    DateTime? DueOn,
    int? BadgerFrequencyInMinutes,
    string? BadgerMessages,
    TaskRepeaterDto? Repeater
);
