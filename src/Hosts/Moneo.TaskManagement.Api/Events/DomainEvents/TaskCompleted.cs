using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskDomainCompleted(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskCompletedHandler : INotificationHandler<TaskDomainCompleted>
{
    private readonly ILogger<TaskCompletedHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskCompletedHandler(ILogger<TaskCompletedHandler> logger, ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }

    public async Task Handle(TaskDomainCompleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} completed", notification.Task.Id);

        var scheduler = _schedulerService.GetScheduler();

        if (scheduler is null)
        {
            _logger.LogError("Scheduler is not available");
        }

        var task = notification.Task;

        var badgerJobKey = task.GetBadgerJobKey();

        if (await scheduler!.DeleteJob(badgerJobKey, cancellationToken))
        {
            _logger.LogInformation("Badger job for task {TaskId} deleted", task.Id);
        }
    }
}
