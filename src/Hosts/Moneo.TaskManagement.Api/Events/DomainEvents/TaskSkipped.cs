using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskSkipped(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskSkippedHandler : INotificationHandler<TaskSkipped>
{
    private readonly ILogger<TaskSkippedHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskSkippedHandler(ILogger<TaskSkippedHandler> logger, ISchedulerService schedulerService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
    }

    public async Task Handle(TaskSkipped notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} skipped", notification.Task.Id);

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
