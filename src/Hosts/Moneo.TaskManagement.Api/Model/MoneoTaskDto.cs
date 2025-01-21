namespace Moneo.TaskManagement.Model;
/*

public enum MoneoTaskEventType
{
    Created,
    Completed,
    Skipped,
    Deactivated,
    Activated,
}

public record MoneoTaskDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> CompletedMessages { get; set; } = [];
    public bool CanBeSkipped { get; set; }
    public IReadOnlyList<string> SkippedMessages { get; set; } = [];
    public string Timezone { get; set; } = "";
    public DateTimeOffset? DueOn { get; set; }
    public int? BadgerFrequencyInMinutes { get; set; }
    public IReadOnlyList<string> BadgerMessages { get; set; } = [];
    public TaskRepeaterDto? Repeater { get; set; }
}

public record MoneoTaskHistoryRecordDto
{
    public DateTimeOffset Timestamp { get; init; }
    public MoneoTaskEventType EventType { get; init; }
}

public record MoneoTaskWithHistoryDto : MoneoTaskDto
{
    public MoneoTaskWithHistoryDto(MoneoTaskDto taskDto, IEnumerable<MoneoTaskHistoryRecordDto> historyRecords)
    {
        Id = taskDto.Id;
        Name = taskDto.Name;
        Description = taskDto.Description;
        IsActive = taskDto.IsActive;
        CompletedMessages = taskDto.CompletedMessages;
        CanBeSkipped = taskDto.CanBeSkipped;
        SkippedMessages = taskDto.SkippedMessages;
        Timezone = taskDto.Timezone;
        DueOn = taskDto.DueOn;
        BadgerFrequencyInMinutes = taskDto.BadgerFrequencyInMinutes;
        BadgerMessages = taskDto.BadgerMessages;
        Repeater = taskDto.Repeater;
        History = historyRecords.ToList();
    }

    public IReadOnlyList<MoneoTaskHistoryRecordDto> History { get; set; } = [];
}

public record TaskRepeaterDto
{
    public string RepeatCron { get; }
    public DateTimeOffset? Expiry { get; }
    public int? EarlyCompletionThresholdHours { get; }

    public TaskRepeaterDto(string repeatCron, DateTimeOffset? expiry, int? earlyCompletionThresholdHours)
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
*/