using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;
using Quartz;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskPastDue(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskPastDueEventHandler : INotificationHandler<TaskPastDue>
{
    private readonly ILogger<TaskPastDueEventHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskPastDueEventHandler(ILogger<TaskPastDueEventHandler> logger, ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }
    
    public async Task Handle(TaskPastDue notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling TaskPastDue event for task {TaskId}", notification.Task.Id);
        
        var scheduler = _schedulerService.GetScheduler();

        if (scheduler is null)
        {
            _logger.LogError("Scheduler is not available");
        }
        
        var scheduleResult = await notification.Task.ScheduleBadgerJobAsync(scheduler, cancellationToken);

        if (!scheduleResult.IsSuccess)
        {
            _logger.LogError(
                scheduleResult.Exception, 
                "Failed to schedule badger job for task {TaskId}: {Error}",
                notification.Task.Id, 
                scheduleResult.Message);
        }
        
        _logger.LogInformation("Badger job scheduled for task {TaskId}", notification.Task.Id);
    }
}
