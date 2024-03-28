using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Models;

namespace Moneo.Functions.Isolated.TaskManager;

internal interface IDurableEntityTasksService
{
    Task<TaskFunctionResult> CompleteOrSkipTaskAsync(TaskFullId taskFullId, bool isSkipped, DurableTaskClient client);
    Task CreateOrModifyTaskAsync(TaskFullId taskFullId, DurableTaskClient client, string method, MoneoTaskDto task);
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

    public async Task CreateOrModifyTaskAsync(TaskFullId taskFullId, DurableTaskClient client, string method, MoneoTaskDto task)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, method, task);
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
