using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Api.Services;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;
using Quartz;

namespace Moneo.TaskManagement.Jobs;

[DisallowConcurrentExecution]
internal sealed class CheckSendJob : IJob
{
    private readonly ILogger<CheckSendJob> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly MoneoTasksDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly IPublisher _publisher;

    public CheckSendJob(
        ILogger<CheckSendJob> logger, 
        TimeProvider timeProvider, 
        MoneoTasksDbContext dbContext, 
        INotificationService notificationService,
        IPublisher publisher)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _publisher = publisher;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Executing job {JobKey}", context.JobDetail.Key);
        var dataMap = context.Trigger.JobDataMap;

        var taskId = dataMap.GetLong(JobConstants.Tasks.Id);
        var message = dataMap.GetString(JobConstants.Message);

        _logger.LogDebug("TaskId: {TaskId}, Message: {Message}", taskId, message);

        var taskInfo = await _dbContext.Tasks.GetTaskWithHistoryDataAsync(taskId);

        if (taskInfo is null)
        {
            await HandleTaskNotFound(context, taskId);
            return;
        }

        if (!taskInfo.IsActive)
        {
            await HandleTaskDisabled(context, taskId);
            return;
        }

        message ??= $"Your task {taskInfo.Name} is due now!";

        if (ShouldSkipNotification(taskInfo))
        {
            _logger.LogInformation(
                "Task was completed within early completion threshold, skipping send for TaskId: {TaskId}", taskId);
            return;
        }

        try
        {
            await _publisher.Publish(new TaskPastDue(_timeProvider.GetUtcNow().UtcDateTime, taskId));
            
            await SendNotificationAsync(taskInfo.ConversationId, message);
            _logger.LogInformation("Notification sent for TaskId: {TaskId}", taskId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send notification for TaskId: {TaskId}", taskId);
        }
    }

    private async Task HandleTaskNotFound(IJobExecutionContext context, long taskId)
    {
        _logger.LogError("Task not found for TaskId: {TaskId}", taskId);
        await context.Scheduler.UnscheduleJob(context.Trigger.Key);
    }

    private async Task HandleTaskDisabled(IJobExecutionContext context, long taskId)
    {
        _logger.LogWarning("Task is disabled, skipping send for TaskId: {TaskId}", taskId);
        await context.Scheduler.UnscheduleJob(context.Trigger.Key);
    }

    private bool ShouldSkipNotification(MoneoTaskCompletionDataDto taskInfo)
    {
        if (taskInfo.LastCompletedOrSkipped is null)
        {
            return false;
        }
        
        if (taskInfo.Repeater is null)
        {
            return true;
        }

        return _timeProvider.GetUtcNow().UtcDateTime.Subtract(taskInfo.LastCompletedOrSkipped.Value) <=
               TimeSpan.FromHours(taskInfo.Repeater.EarlyCompletionThresholdHours);
    }

    private async Task SendNotificationAsync(long conversationId, string message, CancellationToken cancellationToken = default)
    {
        await _notificationService.SendTextNotification(conversationId, message, false, cancellationToken);
    }
}