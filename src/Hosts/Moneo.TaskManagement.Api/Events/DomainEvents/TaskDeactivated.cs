using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskDeactivated(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskDeactivatedHandler : INotificationHandler<TaskDeactivated>
{
    private readonly ILogger<TaskDeactivatedHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskDeactivatedHandler(ILogger<TaskDeactivatedHandler> logger, ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }

    public async Task Handle(TaskDeactivated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} deactivated", notification.Task.Id);

        var scheduler = _schedulerService.GetScheduler();

        if (scheduler is null)
        {
            _logger.LogError("Scheduler is not available");
        }

        var task = notification.Task;

        var dueDateJobKey = task.GetDueDateJobKey();
        
        if (await scheduler!.DeleteJob(dueDateJobKey, cancellationToken))
        {
            _logger.LogInformation("Due date job for task {TaskId} deleted", task.Id);
        }
        
        var badgerJobKey = task.GetBadgerJobKey();

        if (await scheduler!.DeleteJob(badgerJobKey, cancellationToken))
        {
            _logger.LogInformation("Badger job for task {TaskId} deleted", task.Id);
        }
    }
}
