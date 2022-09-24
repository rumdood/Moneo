using Moneo.Models;

namespace Moneo.Core;

public interface IMoneoTaskFactory
{
    MoneoTaskDto CreateTaskDto(MoneoTaskWithReminders input);
    MoneoTaskWithReminders CreateTaskWithReminders(MoneoTaskDto input, MoneoTaskWithReminders? previousVersion = null);
}

public class MoneoTaskFactory : IMoneoTaskFactory
{
    private readonly IScheduleManager _scheduleManager;

    public MoneoTaskFactory(IScheduleManager scheduleManager)
    {
        _scheduleManager = scheduleManager;
    }

    public MoneoTaskWithReminders CreateTaskWithReminders(
        MoneoTaskDto input,
        MoneoTaskWithReminders? previousVersion = null)
    {

        var newTask = new MoneoTaskWithReminders
        {
            Name = input.Name,
            Description = input.Description,
            IsActive = true,
            CompletedOn = previousVersion?.CompletedOn,
            SkippedOn = previousVersion?.SkippedOn,
            CompletedMessage = input.CompletedMessage,
            SkippedMessage = input.SkippedMessage,
            Repeater = input.Repeater,
            Badger = input.Badger,
            Reminders = input.Reminders
                .EmptyIfNull()
                .Where(d => d > DateTimeOffset.UtcNow)
                .ToDictionary(d => d.UtcTicks, d => new TaskReminder { DueAt = d.UtcDateTime, IsActive = true }),
            TimeZone = input.TimeZone,
            DueDates = _scheduleManager.GetDueDates(input).ToHashSet(),
            Created = previousVersion is { Created: var created }
                ? created
                : DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        if (previousVersion is { DueDates: var previousDueDates } && previousDueDates.Count > 0)
        {
            newTask.DueDates = _scheduleManager.MergeDueDates(newTask, previousDueDates).ToHashSet();
        }

        return newTask;
    }

    public MoneoTaskDto CreateTaskDto(MoneoTaskWithReminders input)
    {
        return new MoneoTaskDto
        {
            Name = input.Name,
            Description = input.Description,
            IsActive = input.IsActive,
            CompletedOn = input.CompletedOn,
            SkippedOn = input.SkippedOn,
            DueDates = input.DueDates,
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
