using Microsoft.EntityFrameworkCore;
using Moneo.Chat;
using Moneo.TaskManagement.Api.Chat;
using Moneo.TaskManagement.Api.Services;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess;
using Quartz;

namespace Moneo.TaskManagement.Jobs;

[DisallowConcurrentExecution]
internal sealed class BadgerJob : IJob
{
    private readonly ILogger<BadgerJob> _logger;
    private readonly MoneoTasksDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationService _notificationService;

    private record MoneoTaskCompletionDataDto(
        long Id,
        long ConversationId,
        bool IsActive,
        TaskBadgerDto? Badger,
        TaskRepeaterDto? Repeater,
        DateTime? LastCompletedOrSkipped);
    
    public BadgerJob(
        ILogger<BadgerJob> logger,
        MoneoTasksDbContext dbContext,
        TimeProvider timeProvider,
        INotificationService notificationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _notificationService = notificationService;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Executing badger job {JobKey}", context.JobDetail.Key);
        var dataMap = context.JobDetail.JobDataMap;
        
        var taskId = dataMap.GetLong("TaskId");
        
        var taskInfo = await GetTaskWithHistoryData(taskId);
        
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
        
        if (taskInfo.Badger is null)
        {
            _logger.LogWarning("Task does not have a badger, skipping send for TaskId: {TaskId}", taskId);
            return;
        }
        
        if (ShouldSkipNotification(taskInfo))
        {
            _logger.LogInformation(
                "Task was completed within early completion threshold, skipping send for TaskId: {TaskId}", taskId);
            return;
        }
        
        // select a random message from the list of badger messages
        var random = new Random();
        var message = taskInfo.Badger.BadgerMessages[random.Next(taskInfo.Badger.BadgerMessages.Count)];
        
        // send the message to the conversation
        _logger.LogInformation("Sending badger message to conversation {ConversationId} for TaskId: {TaskId}",
            taskInfo.ConversationId, taskInfo.Id);
        
        await SendNotificationAsync(taskInfo.ConversationId, message);
    }
    
    private async Task<MoneoTaskCompletionDataDto?> GetTaskWithHistoryData(long taskId)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new MoneoTaskCompletionDataDto(
                t.Id,
                t.ConversationId,
                t.IsActive,
                t.Badger != null ? t.Badger.ToDto() : null,
                t.Repeater != null ? t.Repeater.ToDto() : null,
                t.TaskEvents
                    .Where(h => h.Type == TaskEventType.Completed || h.Type == TaskEventType.Skipped)
                    .OrderByDescending(h => h.OccurredOn)
                    .Select(h => (DateTime?)h.OccurredOn)
                    .FirstOrDefault()
            ))
            .FirstOrDefaultAsync();

        return task;
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
    
    private async Task SendNotificationAsync(
        long conversationId, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        await _notificationService.SendTextNotification(conversationId, message, false, cancellationToken);
    }
}