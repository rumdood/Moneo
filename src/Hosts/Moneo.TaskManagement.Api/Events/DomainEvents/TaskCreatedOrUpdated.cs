using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskCreatedOrUpdated(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskCreatedOrUpdatedEventHandler : INotificationHandler<TaskCreatedOrUpdated>
{
    private readonly ILogger<TaskCreatedOrUpdatedEventHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskCreatedOrUpdatedEventHandler(
        ILogger<TaskCreatedOrUpdatedEventHandler> logger,
        ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }
    
    public async Task Handle(TaskCreatedOrUpdated notification, CancellationToken cancellationToken)
    {
        var scheduler = _schedulerService.GetScheduler();
        
        if (scheduler is null)
        {
            _logger.LogError("Scheduler is not available");
            return;
        }

        var scheduleResult = await notification.Task.ScheduleCheckSendJobAsync(scheduler, cancellationToken);
        
        if (!scheduleResult.IsSuccess)
        {
            _logger.LogError(scheduleResult.Exception, 
                "Failed to schedule due job for task {TaskId}: {Message}",
                notification.Task.Id,
                scheduleResult.Message);
        }
        else
        {
            _logger.LogInformation(scheduleResult.Message);
        }
    }
}