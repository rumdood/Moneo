using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;
using Quartz;

namespace Moneo.TaskManagement.Api.Features.ScheduleJobs;

public sealed record ScheduleJobsForTaskRequest(long TaskId): IRequest<JobUpdateResult>;

internal sealed class ScheduleJobsForTaskRequestHandler(
    MoneoTasksDbContext dbContext, 
    ISchedulerService schedulerService) : IRequestHandler<ScheduleJobsForTaskRequest, JobUpdateResult>
{
    
    private record MoneoTaskCompletionDataDto(
        long Id,
        long ConversationId,
        string Name,
        bool IsActive,
        DateTimeOffset? DueOn,
        TaskRepeaterDto? Repeater,
        DateTimeOffset? LastCompletedOrSkipped);

    private static ITrigger BuildDueTrigger(MoneoTaskCompletionDataDto taskInfo)
    {
        // create a new job
        var jobData = new JobDataMap
        {
            {"TaskId", taskInfo.Id},
            {"Message", $"Your task '{taskInfo.Name}' is due now"},
            {"IsBadger", false}
        };
        
        if (taskInfo is { Repeater: { } repeater })
        {
            // create a CRON trigger for the job
            return TriggerBuilder.Create()
                .WithMoneoIdentity(taskInfo.Id, CheckSendType.Due)
                .UsingJobData(jobData)
                .WithCronSchedule(repeater.CronExpression)
                .EndAt(repeater.Expiry)
                .Build();
        }

        if (!taskInfo.DueOn.HasValue)
        {
            throw new InvalidOperationException("Task does not have a due date");
        }

        return TriggerBuilder.Create()
            .WithMoneoIdentity(taskInfo.Id, CheckSendType.Due)
            .UsingJobData(jobData)
            .StartAt(taskInfo.DueOn.Value)
            .Build();
    }
    
    public async Task<JobUpdateResult> Handle(ScheduleJobsForTaskRequest forTaskRequest, CancellationToken cancellationToken)
    {
        var scheduler = schedulerService.GetScheduler();

        if (scheduler is null)
        {
            return JobUpdateResult.Failed("Scheduler is not available");
        }

        var taskInfo = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.Id == forTaskRequest.TaskId)
            .Select(t => new MoneoTaskCompletionDataDto(
                t.Id,
                t.ConversationId,
                t.Name,
                t.IsActive,
                t.DueOn,
                t.Repeater != null ? t.Repeater.ToDto() : null,
                t.TaskEvents
                    .Where(h => h.Type == TaskEventType.Completed || h.Type == TaskEventType.Skipped)
                    .OrderByDescending(h => h.OccurredOn)
                    .Select(h => h.OccurredOn)
                    .FirstOrDefault()
            ))
            .FirstOrDefaultAsync(cancellationToken);
        
        if (taskInfo is null)
        {
            return JobUpdateResult.TaskNotFound();
        }
        
        if (taskInfo.IsActive == false)
        {
            return JobUpdateResult.TaskNotActive();
        }
        
        // update the due-date job
        var jobKey = new JobKey($"task-{taskInfo.Id}", MoneoSchedulerConstants.DueGroup);

        try
        {
            // remove any existing jobs
            await scheduler.DeleteJob(jobKey, cancellationToken);
        
            var job = JobBuilder.Create<CheckSendJob>()
                .WithIdentity(jobKey)
                .PersistJobDataAfterExecution()
                .Build();

            await scheduler.AddJob(job, replace: false, storeNonDurableWhileAwaitingScheduling: true, cancellationToken);

            var dueTrigger = BuildDueTrigger(taskInfo);
        
            // schedule the job with the current jobData
            await scheduler.ScheduleJob(job, dueTrigger, cancellationToken);
        
            return JobUpdateResult.Success();
        }
        catch (Exception e)
        {
            return JobUpdateResult.Failed("An error occurred while updating the job", e);
        }
    }
}
