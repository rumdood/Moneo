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
}

public class ScheduleManager : IScheduleManager
{
    public const int MaxDueDatesToSchedule = 2;

    private readonly CrontabSchedule.ParseOptions _parseOptions = new() { IncludingSeconds = true };

    /// <summary>
    /// Gets the UTC due dates for a task based on the given start and end times and the CRON expression
    /// </summary>
    /// <param name="input">The moneo task input to base the calculation on.</param>
    /// <returns>A collection of DateTime objects for UTC</returns>
    /// <exception cref="InvalidOperationException"></exception>
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
    
    /// <summary>
    /// Gets the UTC due dates for a task based on the given start and end times and the CRON expression
    /// </summary>
    /// <param name="cronExpression">The CRON expression to configure the repeater</param>
    /// <param name="timeZone">The universal ID string for the configured time zone</param>
    /// <param name="startDate">OPTIONAL: The earlierst date from which to calculate the next due date based on the CRON expression</param>
    /// <param name="maxDate">OPTIONAL: The latest date for which to calculate the next due date based on the CRON expression</param>
    /// <param name="max">OPTIONAL: The maximum number of due date instances to calculate</param>
    /// <returns>A collection of DateTime objects for UTC</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone, DateTime? startDate, DateTime? maxDate = null, int max = MaxDueDatesToSchedule)
    {
        if (string.IsNullOrEmpty(cronExpression))
        {
            throw new InvalidOperationException("CRON Expression Cannot Be Null");
        }

        var tz = string.IsNullOrEmpty(timeZone) 
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(timeZone);

        var next = startDate.HasValue
            ? startDate.Value
            : DateTime.UtcNow.UniversalTimeToTimeZone(tz);

        for (var i = 0; i < max; i++)
        {
            next = CrontabSchedule.Parse(cronExpression, _parseOptions).GetNextOccurrence(next);

            if (maxDate.HasValue && next > maxDate.Value)
            {
                break;
            }

            yield return tz.Equals(TimeZoneInfo.Utc) ? next : next.ToUniversalTime(tz);
        }
    }

    /// <summary>
    /// Gets the UTC due dates for a task based on the given start and end times and the CRON expression
    /// </summary>
    /// <param name="cronExpression">The CRON expression to configure the repeater</param>
    /// <param name="timeZone">The universal ID string for the configured time zone</param>
    /// <param name="maxDate">OPTIONAL: The latest date for which to calculate the next due date based on the CRON expression</param>
    /// <param name="max">OPTIONAL: The maximum number of due date instances to calculate</param>
    /// <returns>A collection of DateTime objects for UTC</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IEnumerable<DateTime> GetDueDates(string cronExpression, string timeZone = "", DateTime? maxDate = null, int max = MaxDueDatesToSchedule)
        => GetDueDates(cronExpression, timeZone, DateTime.Now, maxDate, max);

    public IEnumerable<DateTime> GetKeepers(IMoneoTask task, IEnumerable<DateTime> oldDueDates)
    {
        if (task.Repeater is not { RepeatCron: var cron, Expiry: var expiry })
        {
            return Enumerable.Empty<DateTime>();
        }
        
        return oldDueDates.Where(dd => !task.DueDates.Contains(dd) && task.IsValidDueDate(dd));
    }

    /// <summary>
    /// Merges a collection of DueDates with a MoneoTask, keeping only those that are valid based on the current CRON expression
    /// </summary>
    /// <param name="task"></param>
    /// <param name="oldDueDates"></param>
    /// <returns>A collection of DateTime objects for UTC</returns>
    public IEnumerable<DateTime> MergeDueDates(IMoneoTask task, IEnumerable<DateTime> oldDueDates)
    {
        var keepers = GetKeepers(task, oldDueDates);        
        return task.DueDates.Union(keepers);
    }
}
