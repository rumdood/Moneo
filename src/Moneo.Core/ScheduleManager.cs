using Moneo.Models;
using NCrontab;

namespace Moneo.Core;

public interface IScheduleManager
{
    IEnumerable<DateTime> GetDueDates(IMoneoTask input);
    IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? maxDate, int max);
    IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? startDate, DateTime? maxDate, int max);
}

public class ScheduleManager : IScheduleManager
{
    private readonly CrontabSchedule.ParseOptions _parseOptions = new CrontabSchedule.ParseOptions { IncludingSeconds = true };

    public const int MaxDueDatesToSchedule = 10;

    public IEnumerable<DateTime> GetDueDates(IMoneoTask input)
    {
        if (input.Repeater is not { Expiry: var expiry, RepeatCron: var cron })
        {
            if (input.DueDates.Count == 0)
            {
                throw new InvalidOperationException("Task must have at least one due date");
            }

            return input.DueDates;
        }

        return GetDueDates(cron, input.TimeZone ?? "", maxDate: expiry);
    }

    public IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone = "", DateTime? maxDate = null, int max = MaxDueDatesToSchedule)
        => GetDueDates(cronExpression, timeZone, DateTime.Now, maxDate, max);

    public IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? startDate, DateTime? maxDate = null, int max = MaxDueDatesToSchedule)
    {
        if (string.IsNullOrEmpty(cronExpression))
        {
            throw new InvalidOperationException("CRON Expression Cannot Be Null");
        }

        var tz = string.IsNullOrEmpty(timeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone) 
            : TimeZoneInfo.Local;

        var next = startDate.HasValue
            ? GetTimeZoneAdjustedDateTime(startDate.Value, tz)
            : GetTimeZoneAdjustedDateTime(DateTime.Now, tz);

        for (var i = 0; i < max; i++)
        {
            if (maxDate.HasValue && next > maxDate.Value)
            {
                break;
            }

            next = CrontabSchedule.Parse(cronExpression, _parseOptions).GetNextOccurrence(next);
            yield return tz == TimeZoneInfo.Local ? next : next.ToLocalTime();
        }
    }

    private DateTime GetTimeZoneAdjustedDateTime(DateTime dateTime, TimeZoneInfo timeZone)
    {
        var dtUnspec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dtUnspec, timeZone);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}
