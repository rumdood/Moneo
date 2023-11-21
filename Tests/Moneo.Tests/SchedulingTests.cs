using Moneo.Common;
using Moneo.Core;
using Moneo.Models;
using Moneo.Models.TaskManagement;
using NCrontab;

namespace Moneo.Tests;

public class SchedulingTests
{
    private readonly IScheduleManager _scheduleManager;
    private readonly IMoneoTaskFactory _taskFactory;
    private readonly MoneoTaskDto _inputDto;
    private readonly List<DateTime> _expectedTenAm;
    private readonly List<DateTime> _expectedElevenAm;
    private const string EST = "Eastern Standard Time";

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
            TimeZone = EST,
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
        for (var i = 0; i < ScheduleManager.MaxDueDatesToSchedule; i++)
        {
            var day = DateTime.Now.AddDays(dayOffset + i);
            var localDate = new DateTime(day.Year, day.Month, day.Day, 10, 0, 0);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(EST);
            _expectedTenAm.Add(localDate.ToUniversalTime(tz));
        }

        _expectedElevenAm = new List<DateTime>();
        for (var i = 0; i < ScheduleManager.MaxDueDatesToSchedule; i++)
        {
            var day = DateTime.Now.AddDays(dayOffset + i);
            var localDate = new DateTime(day.Year, day.Month, day.Day, 11, 0, 0);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(EST);
            _expectedElevenAm.Add(localDate.ToUniversalTime(tz));
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

    [Fact]
    public void ScratchTest()
    {
        var cron = "0 10 * * *";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(EST);

        DateTime? startDate = new DateTime(2022, 12, 25, 0, 0, 0);

        // exptected EST would be 2022-12-25T10:00:00.000-5:00
        // UTC would be 2022-12-25T15:00:00.000+0:00
        DateTime expected = new DateTime(2022, 12, 25, 15, 0, 0);

        var next = startDate.HasValue
            ? startDate.Value
            : DateTime.UtcNow.UniversalTimeToTimeZone(tz);

        next = CrontabSchedule.Parse(cron).GetNextOccurrence(next);

        var nextActual = tz.Equals(TimeZoneInfo.Utc) ? next : next.ToUniversalTime(tz);
        Assert.Equal(expected, nextActual);
    }
}