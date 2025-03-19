using MediatR;
using Moneo.TaskManagement.Api.Services;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskSkipped(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskSkippedHandler : INotificationHandler<TaskSkipped>
{
    private readonly ILogger<TaskSkippedHandler> _logger;
    private readonly ISchedulerService _schedulerService;
    private readonly INotificationService _notificationService;

    public TaskSkippedHandler(
        ILogger<TaskSkippedHandler> logger, 
        ISchedulerService schedulerService,
        INotificationService notificationService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
        _notificationService = notificationService;
    }

    public async Task Handle(TaskSkipped notification, CancellationToken cancellationToken)
    {
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
        
        _logger.LogInformation("Task {TaskId} skipped", notification.Task.Id);
        
        var notifyResult = await _notificationService.SendTextNotification(
            task.ConversationId,
            task.GetRandomSkippedMessage(), 
            cancellationToken: cancellationToken);

        if (!notifyResult.IsSuccess)
        {
            _logger.LogError(notifyResult.Exception, "Failed to send notification for task {TaskId}", task.Id);
        }
    }
}
