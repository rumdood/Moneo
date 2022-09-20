using Moneo.Models;

namespace Moneo.Core;

public interface IMoneoTaskFactory
{
    MoneoTaskDto CreateTaskDto(MoneoTaskWithReminders input);
    MoneoTaskWithReminders CreateTaskWithReminders(MoneoTaskDto input);
}

public class MoneoTaskFactory : IMoneoTaskFactory
{
    private readonly IScheduleManager _scheduleManager;

    public MoneoTaskFactory(IScheduleManager scheduleManager)
    {
        _scheduleManager = scheduleManager;
    }

    public MoneoTaskWithReminders CreateTaskWithReminders(MoneoTaskDto input)
    {
        return new MoneoTaskWithReminders
        {
            Name = input.Name,
            Description = input.Description,
            IsActive = input.IsActive,
            CompletedOn = input.CompletedOn,
            SkippedOn = input.SkippedOn,
            CompletedMessage = input.CompletedMessage,
            SkippedMessage = input.SkippedMessage,
            Repeater = input.Repeater,
            Badger = input.Badger,
            Reminders = input.Reminders
                .EmptyIfNull()
                .Where(d => d > DateTimeOffset.UtcNow)
                .ToDictionary(d => d.UtcTicks, d => new TaskReminder { DueAt = d.UtcDateTime, IsActive = true }),
            TimeZone = input.TimeZone,
            DueDates = _scheduleManager.GetDueDates(input).ToHashSet()
        };
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
                .ToArray()
        };
    }
}
