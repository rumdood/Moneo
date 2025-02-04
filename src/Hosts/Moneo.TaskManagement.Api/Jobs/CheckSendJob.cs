using Microsoft.EntityFrameworkCore;
using Moneo.Chat;
using Moneo.TaskManagement.Api.Chat;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess;
using Quartz;

namespace Moneo.TaskManagement.Jobs;

[DisallowConcurrentExecution]
internal sealed class CheckSendJob : IJob
{
    private readonly ILogger<CheckSendJob> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly MoneoTasksDbContext _dbContext;
    private readonly IChatAdapter _chatAdapter;
    
    private record MoneoTaskCompletionDataDto(
        long Id,
        long ConversationId,
        string Name,
        bool IsActive,
        TaskRepeaterDto? Repeater,
        DateTime? LastCompletedOrSkipped);

    public CheckSendJob(ILogger<CheckSendJob> logger, TimeProvider timeProvider, MoneoTasksDbContext dbContext, IChatAdapter adapter)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _dbContext = dbContext;
        _chatAdapter = adapter;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Executing job {JobKey}", context.JobDetail.Key);
        var dataMap = context.Trigger.JobDataMap;

        var taskId = dataMap.GetLong("TaskId");
        var message = dataMap.GetString("Message");

        _logger.LogDebug("TaskId: {TaskId}, Message: {Message}", taskId, message);

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

        message ??= $"Your task {taskInfo.Name} is due now!";

        if (ShouldSkipNotification(taskInfo))
        {
            _logger.LogInformation(
                "Task was completed within early completion threshold, skipping send for TaskId: {TaskId}", taskId);
            return;
        }

        try
        {
            await SendNotificationAsync(taskInfo.ConversationId, message);
            _logger.LogInformation("Notification sent for TaskId: {TaskId}", taskId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send notification for TaskId: {TaskId}", taskId);
        }
    }

    private async Task<MoneoTaskCompletionDataDto?> GetTaskWithHistoryData(long taskId)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new MoneoTaskCompletionDataDto(
                t.Id,
                t.ConversationId,
                t.Name,
                t.IsActive,
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

    private async Task SendNotificationAsync(long conversationId, string message, CancellationToken cancellationToken = default)
    {
        var botMessage = new BotTextMessageDto(conversationId, message);
        await _chatAdapter.SendBotTextMessageAsync(botMessage, cancellationToken);
    }
}