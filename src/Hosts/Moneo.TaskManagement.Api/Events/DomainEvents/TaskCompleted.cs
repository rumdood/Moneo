using MediatR;
using Moneo.TaskManagement.Api.Services;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskDomainCompleted(DateTime OccuredOn, MoneoTask Task) : TaskDomainEvent(OccuredOn, Task);

internal class TaskCompletedHandler : INotificationHandler<TaskDomainCompleted>
{
    private readonly ILogger<TaskCompletedHandler> _logger;
    private readonly ISchedulerService _schedulerService;
    private readonly INotificationService _notificationService;

    public TaskCompletedHandler(
        ILogger<TaskCompletedHandler> logger, 
        ISchedulerService schedulerService, 
        INotificationService notificationService)
    {
        _logger = logger;
        _schedulerService = schedulerService;
        _notificationService = notificationService;
    }

    public async Task Handle(TaskDomainCompleted notification, CancellationToken cancellationToken)
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
        
        _logger.LogInformation("Task {TaskId} completed", notification.Task.Id);
        
        var notifyResult = await _notificationService.SendTextNotification(
            task.ConversationId, 
            task.GetRandomCompletedMessage(), 
            cancellationToken: cancellationToken);

        if (!notifyResult.IsSuccess)
        {
            _logger.LogError(notifyResult.Exception, "Failed to send notification for task {TaskId}", task.Id);
        }
    }
}
