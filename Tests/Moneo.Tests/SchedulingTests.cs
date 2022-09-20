using Moneo.Core;
using Moneo.Models;

namespace Moneo.Tests;

public class SchedulingTests
{
    private readonly IScheduleManager _scheduleManager;
    private readonly IMoneoTaskFactory _taskFactory;
    private readonly MoneoTaskDto _inputDto;
    private readonly List<DateTime> _expectedTenAm;
    private readonly List<DateTime> _expectedElevenAm;
    private const string PST = "Pacific Standard Time";

    public SchedulingTests()
    {
        _scheduleManager = new ScheduleManager();
        _taskFactory = new MoneoTaskFactory(_scheduleManager);
        _inputDto = new MoneoTaskDto
        {
            Name = "TestName",
            Description = "Test description",
            CompletedMessage = "Task Completed Message",
            SkippedMessage = "Task Skipped Message",
            TimeZone = PST,
            Repeater = new()
            {
                Expiry = DateTime.Now.AddMonths(1),
                RepeatCron = "0 0 10 * * *"
            },
            Badger = new()
            {
                BadgerFrequencyMinutes = 60,
                BadgerMessages = new[] { "Badger Message 1", "Badger Message 2" }
            }
        };

        var dayOffset = DateTime.Now.Hour < 10 ? 0 : 1; 
        _expectedTenAm = new List<DateTime>();
        for (var i = 0; i < 10; i++)
        {
            var day = DateTime.Now.AddDays(dayOffset + i);
            var localDate = new DateTime(day.Year, day.Month, day.Day, 10, 0, 0);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(PST);
            _expectedTenAm.Add(GetTimeZoneAdjustedDateTime(localDate, tz));
        }

        _expectedElevenAm = new List<DateTime>();
        for (var i = 0; i < 10; i++)
        {
            var day = DateTime.Now.AddDays(dayOffset + i);
            var localDate = new DateTime(day.Year, day.Month, day.Day, 11, 0, 0);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(PST);
            _expectedTenAm.Add(GetTimeZoneAdjustedDateTime(localDate, tz));
        }
    }

    [Fact]
    public void GetDueDates_Yields_ExpectedDates()
    {
        var scheduledDates = _scheduleManager.GetDueDates(_inputDto).ToArray();

        Assert.All(scheduledDates,
            item => Assert.Contains(item, _expectedTenAm));
        Assert.All(scheduledDates,
            item => Assert.DoesNotContain(item, _expectedElevenAm));
    }

    private static DateTime GetTimeZoneAdjustedDateTime(DateTime dateTime, TimeZoneInfo timeZone)
    {
        var dtUnspec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dtUnspec, timeZone);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}