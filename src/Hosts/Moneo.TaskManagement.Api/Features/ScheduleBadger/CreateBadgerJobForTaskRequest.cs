using MediatR;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;
using Quartz;

namespace Moneo.TaskManagement.Api.Features.ScheduleBadger;

public record CreateBadgerJobForTaskRequest(long TaskId, int BadgerIntervalInMinutes): IRequest<JobUpdateResult>;

internal sealed class CreateBadgerJobForTaskRequestHandler(MoneoTasksDbContext dbContext, ISchedulerService schedulerService)
    : IRequestHandler<CreateBadgerJobForTaskRequest, JobUpdateResult>
{
    public async Task<JobUpdateResult> Handle(CreateBadgerJobForTaskRequest request, CancellationToken cancellationToken)
    {
        var scheduler = schedulerService.GetScheduler();

        if (scheduler is null)
        {
            return JobUpdateResult.Failed("Scheduler is not available");
        }
        
        var task = await dbContext.Tasks.FindAsync([request.TaskId], cancellationToken: cancellationToken);
        if (task is null)
        {
            return JobUpdateResult.TaskNotFound();
        }
        
        if (task.IsActive == false)
        {
            return JobUpdateResult.TaskNotActive();
        }

        try
        {
            // update the badger job
            var jobKey = new JobKey($"task-badger-{task.Id}", MoneoSchedulerConstants.BadgerGroup);
        
            var jobData = new JobDataMap
            {
                {"TaskId", task.Id},
            };

            var job = JobBuilder.Create<BadgerJob>()
                .WithIdentity(jobKey)
                .PersistJobDataAfterExecution()
                .Build();
        
            await scheduler.AddJob(job, replace: false, storeNonDurableWhileAwaitingScheduling: true, cancellationToken);
        
            var trigger = TriggerBuilder.Create()
                .WithMoneoIdentity(task.Id, CheckSendType.Badger)
                .UsingJobData(jobData)
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(request.BadgerIntervalInMinutes)
                    .RepeatForever())
                .ForJob(job)
                .Build();
        
            await scheduler.ScheduleJob(trigger, cancellationToken);

            return JobUpdateResult.Success();
        }
        catch (Exception e)
        {
            return JobUpdateResult.Failed(e);
        }
    }
}