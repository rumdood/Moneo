using Moneo.Models;
using NCrontab;
using static NCrontab.CrontabSchedule;

namespace Moneo.Core;

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
}