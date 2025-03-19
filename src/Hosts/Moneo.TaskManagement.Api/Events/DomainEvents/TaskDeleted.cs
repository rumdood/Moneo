using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskDeleted(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskDeletedHandler : INotificationHandler<TaskDeleted>
{
    private readonly ILogger<TaskDeletedHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskDeletedHandler(ILogger<TaskDeletedHandler> logger, ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }

    public async Task Handle(TaskDeleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} deleted", notification.Task.Id);

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
