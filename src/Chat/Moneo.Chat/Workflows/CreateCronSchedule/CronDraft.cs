using System.Globalization;
using System.Text;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

internal sealed class CronDraft
{
    private readonly HashSet<DayOfWeek> _daysOfWeekToRepeat = new();
    private readonly HashSet<TimeOnly> _timesToRepeat = new();
    private readonly HashSet<int> _daysToRepeat = new();
    
    public DayRepeatMode DayRepeatMode { get; set; }
    public bool IsDaysToRepeatComplete { get; set; }
    public bool IsTimesToRepeatComplete { get; set; }
    public int HourIntervalToRepeat { get; set; }

    public IEnumerable<DayOfWeek> DaysOfWeekToRepeat => _daysOfWeekToRepeat;
    public IEnumerable<TimeOnly> TimesToRepeat => _timesToRepeat;
    public IEnumerable<int> DaysToRepeat => _daysToRepeat;
    
    public void AddRepeatTime(string timeToRepeat)
    {
        string[] formats = {
            "h:mmtt", "h:mmt", // 1:41pm or 1:41p
            "hh:mmtt", "hh:mmt", // 01:41pm or 01:41p
            "h:mm tt", "h:mm t", // 1:41 PM or 1:41 P
            "hh:mm tt", "hh:mm t", // 01:41 PM or 01:41 P
            "H:mm", "HH:mm" // 13:41 or 01:41
        };
        
        foreach (var format in formats)
        {
            if (!TimeOnly.TryParseExact(
                    timeToRepeat,
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                continue;
            }

            _timesToRepeat.Add(parsedTime);
            break;
        }
    }
    
    public void AddRepeatDayOfWeek(string day)
    {
        if (DayRepeatMode != DayRepeatMode.DayOfWeek)
        {
            throw new InvalidOperationException("Cannot add a weekly repeat day");
        }
        
        if (!Enum.TryParse(day, true, out DayOfWeek weekday))
        {
            throw new InvalidOperationException("Invalid day value");
        }

        AddRepeatDayOfWeek(weekday);
    }

    public void AddRepeatDayOfWeek(DayOfWeek day)
    {
        if (DayRepeatMode != DayRepeatMode.DayOfWeek)
        {
            throw new InvalidOperationException("Cannot add a weekly repeat day");
        }
        
        _daysOfWeekToRepeat.Add(day);
        _daysToRepeat.Add((int)day);
    }

    public void AddRepeatDayOfMonth(int day)
    {
        if (DayRepeatMode != DayRepeatMode.DayOfMonth)
        {
            throw new InvalidOperationException("Cannot add a monthly repeat day");
        }
        
        if (day is < 1 or > 31)
        {
            throw new InvalidOperationException("Day must be a value between 1 and 31");
        }

        _daysToRepeat.Add(day);
    }

    public string GenerateCronStatement()
    {
        if (!IsTimesToRepeatComplete || _timesToRepeat.Count == 0)
        {
            throw new InvalidOperationException("CRON Statement must have a time to run");
        }

        var builder = new StringBuilder("0 ");
        var minutes = _timesToRepeat.Select(t => t.Minute).Distinct();
        var hours = _timesToRepeat.Select(t => t.Hour).Distinct();
        builder.Append(string.Join(',', minutes));
        builder.Append(' ');
        builder.Append(string.Join(',', hours));
        builder.Append(' ');
        builder.Append(GetDaysAndMonths(DayRepeatMode, _daysToRepeat));

        return builder.ToString();
    }

    private static string GetDaysAndMonths(DayRepeatMode repeatMode, IEnumerable<int>? days = null)
    {
        if (repeatMode == DayRepeatMode.Daily || days is null)
        {
            return "* * *";
        }

        var dayValues = string.Join(',', days);

        return repeatMode switch
        {
            DayRepeatMode.DayOfMonth => $"{dayValues} * *",
            DayRepeatMode.DayOfWeek => $"* * {dayValues}",
            _ => throw new InvalidOperationException("Unexpected DayRepeatMode")
        };
    }
}
