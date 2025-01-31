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
        // create a new job
        var jobData = new JobDataMap
        {
            {"TaskId", task.Id},
            {"Message", $"Your task '{task.Name}' is due now"},
            {"IsBadger", false}
        };
        
        if (task is { Repeater: { } repeater })
        {
            // create a CRON trigger for the job
            return TriggerBuilder.Create()
                .WithMoneoIdentity(task.Id, CheckSendType.Due)
                .UsingJobData(jobData)
                .WithCronSchedule(repeater.CronExpression)
                .EndAt(repeater.Expiry)
                .Build();
        }

        if (!task.DueOn.HasValue)
        {
            throw new InvalidOperationException("Task does not have a due date");
        }

        return TriggerBuilder.Create()
            .WithMoneoIdentity(task.Id, CheckSendType.Due)
            .UsingJobData(jobData)
            .StartAt(task.DueOn.Value)
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

            await scheduler.AddJob(
                job, 
                replace: false, 
                storeNonDurableWhileAwaitingScheduling: true, 
                cancellationToken);

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
                {"TaskId", task.Id},
            };

            var job = JobBuilder.Create<BadgerJob>()
                .WithIdentity(jobKey)
                .PersistJobDataAfterExecution()
                .Build();

            await scheduler.AddJob(
                job, 
                replace: false, 
                storeNonDurableWhileAwaitingScheduling: true,
                cancellationToken);
        
            var trigger = TriggerBuilder.Create()
                .WithMoneoIdentity(task.Id, CheckSendType.Badger)
                .UsingJobData(jobData)
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(task.Badger.BadgerFrequencyInMinutes)
                    .RepeatForever())
                .ForJob(job)
                .Build();
        
            await scheduler.ScheduleJob(trigger, cancellationToken);

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
    }

    internal static string GetQuartzCronExpression(this string cronExpression)
    {
        if (Quartz.CronExpression.IsValidExpression(cronExpression))
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
        
        if (quartzCron is { DayOfWeek: "*", DayOfMonth: "*" })
        {
            quartzCron.DayOfWeek = "?";
        }

        var quartzCronString =
            $"{quartzCron.Seconds} {quartzCron.Minutes} {quartzCron.Hours} {quartzCron.DayOfMonth} {quartzCron.Month} {quartzCron.DayOfWeek} {quartzCron.Year}";

        return quartzCronString;
    }
}
