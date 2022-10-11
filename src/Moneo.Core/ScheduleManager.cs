using Moneo.Models;
using NCrontab;

namespace Moneo.Core;

public interface IScheduleManager
{
    IEnumerable<DateTime> GetDueDates(IMoneoTask input);
    IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? maxDate, int max);
    IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? startDate, DateTime? maxDate, int max);
    IEnumerable<DateTime> GetKeepers(IMoneoTask task, IEnumerable<DateTime> oldDueDates);
    IEnumerable<DateTime> MergeDueDates(IMoneoTask task, IEnumerable<DateTime> oldDueDates);
    bool IsValidDueDateForTask(DateTime dueDate, IMoneoTask task);
}

public class ScheduleManager : IScheduleManager
{
    public const int MaxDueDatesToSchedule = 2;

    private readonly CrontabSchedule.ParseOptions _parseOptions = new() { IncludingSeconds = true };

    public IEnumerable<DateTime> GetDueDates(IMoneoTask input)
    {
        if (input.Repeater is {Expiry: var expiry, RepeatCron: var cron})
        {
            return GetDueDates(cron, input.TimeZone ?? "", maxDate: expiry);
        }

        if (input.DueDates.Count == 0)
        {
            throw new InvalidOperationException("Task must have at least one due date");
        }

        return input.DueDates;

    }

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
            ? startDate.Value.GetTimeZoneAdjustedDateTime(tz)
            : DateTime.Now.GetTimeZoneAdjustedDateTime(tz);

        for (var i = 0; i < max; i++)
        {
            next = CrontabSchedule.Parse(cronExpression, _parseOptions).GetNextOccurrence(next);

            if (maxDate.HasValue && next > maxDate.Value)
            {
                break;
            }

            yield return tz.Equals(TimeZoneInfo.Local) ? next : next.ToLocalTime();
        }
    }

    public IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone = "", DateTime? maxDate = null, int max = MaxDueDatesToSchedule)
        => GetDueDates(cronExpression, timeZone, DateTime.Now, maxDate, max);

    public IEnumerable<DateTime> GetKeepers(IMoneoTask task, IEnumerable<DateTime> oldDueDates)
    {
        if (task.Repeater is not { RepeatCron: var cron, Expiry: var expiry })
        {
            return Enumerable.Empty<DateTime>();
        }
        
        return oldDueDates.Where(dd => !task.DueDates.Contains(dd) && IsValidDueDate(dd, cron, expiry));
    }

    public IEnumerable<DateTime> MergeDueDates(IMoneoTask task, IEnumerable<DateTime> oldDueDates)
    {
        var keepers = GetKeepers(task, oldDueDates);        
        return task.DueDates.Union(keepers);
    }

    public bool IsValidDueDateForTask(DateTime dueDate, IMoneoTask task)
    {
        return task.Repeater is { RepeatCron: var cron, Expiry: var expiry } 
            ? IsValidDueDate(dueDate, cron, expiry) 
            : task.DueDates.Contains(dueDate);
    }

    private bool IsValidDueDate(DateTime dueDate, string cronExpression, DateTime? maxDate = null)
    {
        var early = dueDate.AddSeconds(-10);
        var expected = CrontabSchedule.Parse(cronExpression, _parseOptions).GetNextOccurrence(early);

        return dueDate == expected 
            && (!maxDate.HasValue || maxDate.Value > expected);
    }
}
