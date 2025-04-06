using Moneo.Common;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;
using Quartz;

namespace Moneo.TaskManagement.Jobs;

public static class MoneoTaskExtensions
{
    public static JobKey GetDueDateJobKey(this MoneoTask task)
    {
        return new JobKey($"task-{task.Id}", MoneoSchedulerConstants.DueGroup);
    }
    
    public static JobKey GetBadgerJobKey(this MoneoTask task)
    {
        return new JobKey($"task-badger-{task.Id}", MoneoSchedulerConstants.BadgerGroup);
    }
    
    private static ITrigger BuildDueTrigger(this MoneoTask task)
    {
        if (task.Conversation is null && task.Id == 0)
        {
            throw new InvalidOperationException("Task does not have a conversation or an ID");
        }
        
        var conversationId = task.Conversation?.Id ?? task.ConversationId;
        
        // create a new job
        var jobData = new JobDataMap
        {
            {JobConstants.Tasks.ConversationId, conversationId},
            {JobConstants.Tasks.Id, task.Id},
            {JobConstants.Tasks.Name, task.Name},
            {"Message", $"Your task '{task.Name}' is due now"},
        };
        
        if (task is { Repeater: { } repeater })
        {
            var quartzCron = repeater.CronExpression.GetQuartzCronExpression();
            
            // create a CRON trigger for the job
            return TriggerBuilder.Create()
                .WithMoneoIdentity(task.Id, CheckSendType.Due)
                .UsingJobData(jobData)
                .WithCronSchedule(quartzCron, x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(task.Timezone)))
                .EndAt(repeater.Expiry)
                .Build();
        }

        if (!task.DueOn.HasValue)
        {
            throw new InvalidOperationException("Task does not have a due date");
        }
        
        // adjust the DueDate to the task's timezone
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(task.Timezone);
        var dueDate = new DateTimeOffset(task.DueOn.Value, timeZoneInfo.GetUtcOffset(task.DueOn.Value));

        return TriggerBuilder.Create()
            .WithMoneoIdentity(task.Id, CheckSendType.Due)
            .UsingJobData(jobData)
            .StartAt(dueDate)
            .Build();
    }
    
    public static async Task<MoneoResult> ScheduleCheckSendJobAsync(
        this MoneoTask task, 
        IScheduler scheduler, 
        CancellationToken cancellationToken = default)
    {
        // update the due-date job
        var jobKey = task.GetDueDateJobKey();

        try
        {
            // remove any existing jobs
            await scheduler.DeleteJob(jobKey, cancellationToken);
        
            var job = JobBuilder.Create<CheckSendJob>()
                .WithIdentity(jobKey)
                .PersistJobDataAfterExecution()
                .Build();

            var dueTrigger = task.BuildDueTrigger();
        
            // schedule the job with the current jobData
            await scheduler.ScheduleJob(job, dueTrigger, cancellationToken);
            
            var jobType = task.Repeater is null ? "Due-Date" : "Repeating";

            return MoneoResult.Success($"Scheduled {jobType} job for task {task.Id}");
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }

    public static async Task<MoneoResult> ScheduleBadgerJobAsync(
        this MoneoTask task,
        IScheduler scheduler,
        CancellationToken cancellationToken = default)
    {
        var jobKey = task.GetBadgerJobKey();
        
        // remove any existing badger jobs
        await scheduler!.DeleteJob(jobKey, cancellationToken);
        
        if (task.IsActive == false)
        {
            return MoneoResult.NoChange($"Task {task.Id} is not active, no badger jobs scheduled");
        }

        if (task.Badger is null)
        {
            return MoneoResult.NoChange(
                $"Task {task.Id} does not have a badger configuration, no badger jobs scheduled");
        }
        
        try
        {
            // check to see if the job already exists
            if (await scheduler!.CheckExists(jobKey, cancellationToken))
            {
                return MoneoResult.NoChange($"Badger job already exists for task {task.Id}, skipping");
            }
        
            var jobData = new JobDataMap
            {
                { JobConstants.Tasks.Id, task.Id },
                { JobConstants.Tasks.Name, task.Name },
                { JobConstants.Tasks.ConversationId, task.ConversationId },
            };
        
            var trigger = TriggerBuilder.Create()
                .WithMoneoIdentity(task.Id, CheckSendType.Badger)
                .UsingJobData(jobData)
                .StartAt(DateBuilder.FutureDate(task.Badger.BadgerFrequencyInMinutes, IntervalUnit.Minute))
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(task.Badger.BadgerFrequencyInMinutes)
                    .RepeatForever())
                .Build();
            
            var job = JobBuilder.Create<BadgerJob>()
                .WithIdentity(jobKey)
                .PersistJobDataAfterExecution()
                .Build();
        
            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}

public static class CronExpressionExtension
{
    private struct QuartzCronObject
    {
        public string Seconds { get; set; }
        public string Minutes { get; set; }
        public string Hours { get; set; }
        public string DayOfMonth { get; set; }
        public string Month { get; set; }
        public string DayOfWeek { get; set; }
        public string Year { get; set; }
        
        public override string ToString()
        {
            return $"{Seconds} {Minutes} {Hours} {DayOfMonth} {Month} {DayOfWeek} {Year}";
        }
    }

    internal static string GetQuartzCronExpression(this string cronExpression)
    {
        if (CronExpression.IsValidExpression(cronExpression))
        {
            return cronExpression;
        }
        
        var quartzCron = new QuartzCronObject();
        var parts = cronExpression.Split(' ');

        var offset = parts.Length > 5 ? 1 : 0;

        quartzCron.Seconds = parts.Length > 5 ? parts[0] : "0";
        quartzCron.Minutes = parts[offset + 0];
        quartzCron.Hours = parts[offset + 1];
        quartzCron.DayOfMonth = parts[offset + 2];
        quartzCron.Month = parts[offset + 3];
        quartzCron.DayOfWeek = parts[offset + 4];
        quartzCron.Year = parts.Length > 6 ? parts[offset + 5] : "*";

        if (quartzCron.DayOfWeek == "0")
        {
            // quartz doesn't use a zero-based week
            quartzCron.DayOfWeek = "1";
        }
        
        if (quartzCron.DayOfWeek != "*" && quartzCron.DayOfWeek != "?")
        {
            // Quartz does not support both day-of-week and day-of-month being "*"
            quartzCron.DayOfMonth = "?";
        }
        else if (quartzCron is { DayOfWeek: "*", DayOfMonth: "*" })
        {
            quartzCron.DayOfWeek = "?";
        }

        return quartzCron.ToString();
    }
}
