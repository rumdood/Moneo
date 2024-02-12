using Moneo.Core;
using Moneo.TaskManagement.Models;

namespace Moneo.TaskManagement;

public interface IMoneoTaskFactory
{
    MoneoTaskDto CreateTaskDto(MoneoTaskState input);
    MoneoTaskState CreateTaskWithReminders(
        MoneoTaskDto input, 
        MoneoTaskState? previousVersion = null, 
        int maxCompletionHistoryEventCount = 5);
}

public class MoneoTaskFactory : IMoneoTaskFactory
{
    private readonly IScheduleManager _scheduleManager;

    public MoneoTaskFactory(IScheduleManager scheduleManager)
    {
        _scheduleManager = scheduleManager;
    }

    public MoneoTaskState CreateTaskWithReminders(
        MoneoTaskDto input,
        MoneoTaskState? previousVersion = null,
        int maxCompletionHistoryEventCount = 5)
    {

        var newTask = new MoneoTaskState
        {
            Id = input.Id,
            ConversationId = input.ConversationId,
            Name = input.Name,
            Description = input.Description,
            IsActive = true,
            CompletedHistory = previousVersion?.CompletedHistory ?? new FixedLengthList<DateTime?>(maxCompletionHistoryEventCount),
            SkippedHistory = previousVersion?.SkippedHistory ?? new FixedLengthList<DateTime?>(maxCompletionHistoryEventCount),
            CompletedMessage = input.CompletedMessage,
            SkippedMessage = input.SkippedMessage,
            Repeater = input.Repeater,
            Badger = input.Badger,
            Reminders = input.Reminders
                .EmptyIfNull()
                .Where(d => d > DateTimeOffset.UtcNow)
                .ToDictionary(
                    d => d.UtcTicks, 
                    d => new TaskReminder { DueAt = d.UtcDateTime, IsActive = true }),
            TimeZone = input.TimeZone,
            DueDates = _scheduleManager.GetDueDates(input).ToHashSet(),
            Created = previousVersion is { Created: var created }
                ? created
                : DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        if (previousVersion is { DueDates: var previousDueDates } && previousDueDates.Count > 0)
        {
            newTask.DueDates.UnionWith(previousDueDates.Where(dd => dd > DateTime.UtcNow && newTask.IsValidDueDate(dd)));
        }

        return newTask;
    }

    public MoneoTaskDto CreateTaskDto(MoneoTaskState input)
    {
        return new MoneoTaskDto
        {
            Id = input.Id,
            ConversationId = input.ConversationId,
            Name = input.Name,
            Description = input.Description,
            IsActive = input.IsActive,
            CompletedHistory = input.CompletedHistory.Where(x => x is not null).ToArray(),
            SkippedHistory = input.SkippedHistory.Where(x => x is not null).ToArray(),
            DueDates = input.DueDates.Select(x => x.UniversalTimeToTimeZone(input.TimeZone)).ToHashSet(),
            TimeZone = input.TimeZone,
            CompletedMessage = input.CompletedMessage,
            SkippedMessage = input.SkippedMessage,
            Repeater = input.Repeater,
            Badger = input.Badger,
            Reminders = input.Reminders
                .EmptyIfNull()
                .Select(kv => new DateTimeOffset(kv.Value.DueAt))
                .ToArray(),
            Created = input.Created,
            LastUpdated = input.LastUpdated,
        };
    }
}
