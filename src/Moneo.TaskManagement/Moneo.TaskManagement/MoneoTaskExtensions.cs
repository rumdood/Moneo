using Moneo.Core;
using Moneo.TaskManagement.Models;
using NCrontab;
using static NCrontab.CrontabSchedule;

namespace Moneo.TaskManagement;

public static class MoneoTaskExtensions
{
    private static readonly ParseOptions _parseOptions = new() { IncludingSeconds = true };

    public static DateTime? GetLastCompletedOrSkippedDate(this IMoneoTaskState task)
    {
        var completed = task.CompletedHistory.FirstOrDefault();
        var skipped = task.SkippedHistory.FirstOrDefault();

        if (completed.HasValue && skipped.HasValue)
        {
            return completed > skipped ? completed.Value : skipped.Value;
        }

        return completed ?? skipped;
    }

    public static TimeZoneInfo GetTimeZoneInfo(this IMoneoTask task)
    {
        return string.IsNullOrEmpty(task.TimeZone)
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(task.TimeZone);
    }

    public static bool IsValidDueDate(this IMoneoTask task, DateTime dueDate)
    {
        if (task.Repeater is not { RepeatCron: var cron, Expiry: var expiry })
        {
            return task.DueDates.Contains(dueDate);
        }

        // due dates are in UTC, but CRON jobs are not so we have to convert away from UTC
        var timeZone = task.GetTimeZoneInfo();
        var timezoneAdjustedDate = dueDate.UniversalTimeToTimeZone(timeZone);
        var early = timezoneAdjustedDate.AddSeconds(-10);
        var expected = CrontabSchedule.Parse(cron, _parseOptions)
            .GetNextOccurrence(early)
            .ToUniversalTime(timeZone);

        return dueDate == expected
            && (!expiry.HasValue || expiry.Value > expected);
    }
    
    public static MoneoTaskDto ToMoneoTaskDto(this MoneoTaskState input)
    {
        return new MoneoTaskDto
        {
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
