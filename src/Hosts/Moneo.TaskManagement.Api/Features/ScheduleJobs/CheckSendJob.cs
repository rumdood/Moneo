using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess;
using Quartz;

namespace Moneo.TaskManagement.Api.Features.ScheduleJobs;

[DisallowConcurrentExecution]
internal sealed class CheckSendJob : IJob
{
    private readonly ILogger<CheckSendJob> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly MoneoTasksDbContext _dbContext;
    
    private record MoneoTaskCompletionDataDto(
        long Id,
        long ConversationId,
        string Name,
        bool IsActive,
        TaskRepeaterDto? Repeater,
        DateTimeOffset? LastCompletedOrSkipped);

    public CheckSendJob(ILogger<CheckSendJob> logger, TimeProvider timeProvider, MoneoTasksDbContext dbContext)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Executing job {JobKey}", context.JobDetail.Key);
        var dataMap = context.JobDetail.JobDataMap;

        var taskId = dataMap.GetLong("TaskId");
        var message = dataMap.GetString("Message");

        _logger.LogInformation("TaskId: {TaskId}, Message: {Message}", taskId, message);

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
                t.Name,
                t.IsActive,
                t.Repeater != null ? t.Repeater.ToDto() : null,
                t.TaskEvents
                    .Where(h => h.Type == TaskEventType.Completed || h.Type == TaskEventType.Skipped)
                    .OrderByDescending(h => h.OccurredOn)
                    .Select(h => h.OccurredOn)
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

        return _timeProvider.GetUtcNow().Subtract(taskInfo.LastCompletedOrSkipped.Value) <=
               TimeSpan.FromHours(taskInfo.Repeater.EarlyCompletionThresholdHours);
    }

    private Task SendNotificationAsync(long conversationId, string message)
    {
        // send the notification
        return Task.CompletedTask;
    }
}