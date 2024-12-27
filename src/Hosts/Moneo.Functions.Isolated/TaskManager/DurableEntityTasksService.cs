using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Functions.Isolated.TaskManager;

internal interface IDurableEntityTasksService
{
    Task<TaskFunctionResult> CompleteOrSkipTaskAsync(TaskFullId taskFullId, bool isSkipped, DurableTaskClient client);
    Task<TaskFunctionResult> DeactivateTaskAsync(TaskFullId taskFullId, DurableTaskClient client);
    Task<TaskFunctionResult> DeleteTaskAsync(TaskFullId taskFullId, DurableTaskClient client);
    Task<TaskFunctionResult> CreateTaskAsync(TaskFullId taskFullId, DurableTaskClient client, MoneoTaskDto task);
    Task<TaskFunctionResult> UpdateTaskAsync(TaskFullId taskFullId, DurableTaskClient client, MoneoTaskDto task);
    Task<Dictionary<string, MoneoTaskDto>> GetAllTasksDictionaryAsync(DurableTaskClient client);
    Task<Dictionary<string, MoneoTaskDto>> GetAllTasksDictionaryForConversationAsync(string chatId, DurableTaskClient client);
    Task<MoneoTaskDto?> GetTaskDtoAsync(TaskFullId taskFullId, DurableTaskClient client);
}

internal class DurableEntityTasksService : IDurableEntityTasksService
{
    private readonly ILogger<DurableEntityTasksService> _logger;

    public DurableEntityTasksService(ILogger<DurableEntityTasksService> logger)
    {
        _logger = logger;
    }

    private async Task<IReadOnlyList<DurableEntityEntry>> GetAllTaskEntriesAsync(DurableTaskClient client)
    {
        using var tokenSource = new CancellationTokenSource();
        var cancelToken = tokenSource.Token;

        var query = new EntityQuery
        {
            IncludeState = true,
        };

        var results = client.Entities.GetAllEntitiesAsync(query);
        var tasks = new List<DurableEntityEntry>();

        await foreach (var entity in results)
        {
            if (entity.State is not null)
            {
                try
                {
                    tasks.Add(new DurableEntityEntry(entity.Id.Key, entity.State.ReadAs<MoneoTaskState>().ToMoneoTaskDto()));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read teh state for task: {Id}", entity.Id.Key);
                }
            }
        }

        return tasks;
    }

    public async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksDictionaryAsync(DurableTaskClient client)
    {
        var tasks = await GetAllTaskEntriesAsync(client);
        return tasks
                .ToDictionary(dto => dto.Key, dto => dto.TaskDto);
    }

    public async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksDictionaryForConversationAsync(string chatId, DurableTaskClient client)
    {
        var tasks = await GetAllTaskEntriesAsync(client);
        return tasks
                .Where(x => x.Key.Contains('_')) // since there's no way to delete the old ones...
                .Select(x => new
                {
                    FullId = TaskFullId.CreateFromFullId(x.Key),
                    x.TaskDto,
                })
                .Where(x => x.FullId.ChatId.Equals(chatId))
                .ToDictionary(x => x.FullId.TaskId,
                    x => x.TaskDto);
    }

    public async Task<TaskFunctionResult> DeactivateTaskAsync(TaskFullId taskFullId, DurableTaskClient client)
    {
        if (taskFullId is null)
        {
            return new TaskFunctionResult(false, "Task ID Is Required");
        }

        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.DisableTask));
        _logger.LogInformation("{TaskId} Deactivated", taskFullId.TaskId);

        return new TaskFunctionResult(true);
    }

    public async Task<TaskFunctionResult> DeleteTaskAsync(TaskFullId taskFullId, DurableTaskClient client)
    {
        if (taskFullId is null)
        {
            return new TaskFunctionResult(false, "Task ID Is Required");
        }
        
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.Delete));
        _logger.LogInformation("{TaskId} State Deleted", taskFullId.TaskId);
        
        return new TaskFunctionResult(true);
    }

    public async Task<TaskFunctionResult> CreateTaskAsync(TaskFullId taskFullId, DurableTaskClient client, MoneoTaskDto task)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);

        var existing = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);

        if (existing is not null)
        {
            return new TaskFunctionResult(false, "Task ID Conflict");
        }

        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.InitializeTask), task);

        return new TaskFunctionResult(true);
    }
    
    public async Task<TaskFunctionResult> UpdateTaskAsync(TaskFullId taskFullId, DurableTaskClient client, MoneoTaskDto task)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);

        var existing = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);

        if (existing is null)
        {
            return new TaskFunctionResult(false, "Task Not Found");
        }

        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.UpdateTask), task);

        return new TaskFunctionResult(true);
    }

    public async Task<MoneoTaskDto?> GetTaskDtoAsync(TaskFullId taskFullId, DurableTaskClient client)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        var entityState = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);
        return entityState?.State.ToMoneoTaskDto();
    }

    public async Task<TaskFunctionResult> CompleteOrSkipTaskAsync(TaskFullId taskFullId, bool isSkipped, DurableTaskClient client)
    {
        if (taskFullId is null)
        {
            return new TaskFunctionResult(false, "Task ID Is Required");
        }

        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.MarkCompleted), input: isSkipped);
        _logger.LogInformation("Reminder Defused for {TaskId}", taskFullId.TaskId);

        return new TaskFunctionResult(true);
    }
}
