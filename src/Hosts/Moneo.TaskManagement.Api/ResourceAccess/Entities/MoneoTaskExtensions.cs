using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

public static class MoneoTaskExtensions
{
    public static MoneoTaskDto ToDto(this MoneoTask task)
    {
        return new MoneoTaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            IsActive = task.IsActive,
            CompletedMessages = task.CompletedMessages.Split(',').ToList(),
            CanBeSkipped = task.CanBeSkipped,
            SkippedMessages = task.SkippedMessages.Split(',').ToList(),
            Timezone = task.Timezone,
            DueOn = task.DueOn,
            BadgerFrequencyInMinutes = task.BadgerFrequencyInMinutes,
            BadgerMessages = task.BadgerMessages,
            Repeater = task.TaskRepeater != null
                ? new TaskRepeaterDto(task.TaskRepeater.RepeatCron, task.TaskRepeater.Expiry,
                    task.TaskRepeater.EarlyCompletionThresholdHours)
                : null
        };
    }

    public static MoneoTaskWithHistoryDto ToDtoWithHistory(this MoneoTask task,
        IReadOnlyList<MoneoTaskHistoryRecordDto> historyRecordDtos)
    {
        return new MoneoTaskWithHistoryDto
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            IsActive = task.IsActive,
            CompletedMessages = task.CompletedMessages.Split(',').ToList(),
            CanBeSkipped = task.CanBeSkipped,
            SkippedMessages = task.SkippedMessages.Split(',').ToList(),
            Timezone = task.Timezone,
            DueOn = task.DueOn,
            BadgerFrequencyInMinutes = task.BadgerFrequencyInMinutes,
            BadgerMessages = task.BadgerMessages,
            Repeater = task.TaskRepeater != null
                ? new TaskRepeaterDto(task.TaskRepeater.RepeatCron, task.TaskRepeater.Expiry,
                    task.TaskRepeater.EarlyCompletionThresholdHours)
                : null,
            History = historyRecordDtos
        };
    }
}

public static class TaskEventExtensions
{
    public static MoneoTaskHistoryRecordDto ToDto(this TaskEvent taskEvent)
    {
        return new MoneoTaskHistoryRecordDto
        {
            Timestamp = taskEvent.Timestamp,
            EventType = taskEvent.Type switch
            {
                TaskEventType.Completed => MoneoTaskEventType.Completed,
                TaskEventType.Skipped => MoneoTaskEventType.Skipped,
                TaskEventType.Disabled => MoneoTaskEventType.Deactivated,
                _ => throw new ArgumentOutOfRangeException(nameof(taskEvent.Type), "Unknown Task Event Type")
            }
        };
    }
}