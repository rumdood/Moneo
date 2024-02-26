using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal static class HttpVerbs
{
    public const string Delete = "delete";
    public const string Get = "get";
    public const string Patch = "patch";
    public const string Post = "post";
    public const string Put = "put";
}

internal record TaskFunctionResult(bool Success, string? Message = null);

internal class TaskManagerApi
{
    private readonly ILogger<TaskManagerApi> _logger;
    private readonly IMoneoTaskFactory _taskFactory;

    public TaskManagerApi(IMoneoTaskFactory taskFactory, ILogger<TaskManagerApi> log)
    {
        _taskFactory = taskFactory;
        _logger = log;
    }

    private static async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksAsync(DurableTaskClient client)
    {
        using var tokenSource = new CancellationTokenSource();
        var cancelToken = tokenSource.Token;

        var query = new EntityQuery
        {
            IncludeState = true,
        };

        do
        {
            var results = client.Entities.GetAllEntitiesAsync(query);
            var entities = new List<EntityMetadata>();
            await foreach (var entity in results)
            {
                entities.Add(entity);
            }

            if (entities.Count == 0)
            {
                break;
            }

            if (entities != null)
            {
                var tasks = entities
                    .Where(x => x.State is not null);
                return tasks.ToDictionary(x => x.Id.Key, x => x.State.ReadAs<MoneoTaskState>().ToMoneoTaskDto());
            }
        }
        while (query.ContinuationToken != null);

        return new Dictionary<string, MoneoTaskDto>();
    }

    private static async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksForConversationAsync(string chatId, DurableTaskClient client)
    {
        using var tokenSource = new CancellationTokenSource();
        var cancelToken = tokenSource.Token;

        var query = new EntityQuery
        {
            IncludeState = true,
        };

        do
        {
            var results = client.Entities.GetAllEntitiesAsync(query);
            var entities = new List<EntityMetadata>();
            await foreach (var entity in results)
            {
                entities.Add(entity);
            }

            if (entities.Count == 0)
            {
                break;
            }

            if (entities != null)
                return entities
                    .Where(x => x.Id.Key.Contains('_')) // since there's no way to delete the old ones...
                    .Select(x => new
                    {
                        FullId = TaskFullId.CreateFromFullId(x.Id.Key),
                        x.State
                    })
                    .Where(x => x.State is not null && x.FullId.ChatId.Equals(chatId))
                    .ToDictionary(x => x.FullId.TaskId,
                        x => x.State.ReadAs<MoneoTaskState>().ToMoneoTaskDto());
        }
        while (query.ContinuationToken != null);

        return new Dictionary<string, MoneoTaskDto>();
    }

    private async Task CreateOrModifyTaskAsync(TaskFullId taskFullId, DurableTaskClient client, string method, MoneoTaskDto task)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, method, task);
    }

    private async Task<MoneoTaskDto?> GetTaskDtoAsync(TaskFullId taskFullId, DurableTaskClient client)
    {
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        var entityState = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);
        return entityState?.State.ToMoneoTaskDto();
    }

    private async Task<TaskFunctionResult> CompleteOrSkipTaskAsync(TaskFullId taskFullId, bool isSkipped, DurableTaskClient client)
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

    [Function(nameof(CreateTask))]
    public async Task<HttpResponseData> CreateTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "{chatId}/tasks/{taskId}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId)
    {
        var task = await request.ReadFromJsonAsync<MoneoTaskDto>();

        if (task is null)
        {
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        var taskFullId = new TaskFullId(chatId, taskId);
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);

        var existing = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);

        if (existing is not null)
        {
            return request.CreateResponse(HttpStatusCode.Conflict);
        }

        var operationName = nameof(TaskManager.InitializeTask);
        
        await client.Entities.SignalEntityAsync(entityId, operationName, task);
        return request.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(UpdateTask))]
    public async Task<HttpResponseData> UpdateTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Patch, Route = "{chatId}/tasks/{taskId}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId)
    {
        var task = await request.ReadFromJsonAsync<MoneoTaskDto>();

        if (task is null)
        {
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        var taskFullId = new TaskFullId(chatId, taskId);
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);

        var existing = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);

        if (existing is null)
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var operationName = nameof(TaskManager.UpdateTask);
        await client.Entities.SignalEntityAsync(entityId, operationName, task);
        return request.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(CompleteTask))]
    public async Task<HttpResponseData> CompleteTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "{chatId}/tasks/{taskId}/{action}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId,
        string action)
    {
        var skip = action switch
        {
            not null when action.Equals("skip", StringComparison.OrdinalIgnoreCase) => true,
            not null when action.Equals("complete", StringComparison.OrdinalIgnoreCase) => false,
            _ => throw new InvalidOperationException($"Unknown Action: {action}")
        };

        var taskFullId = new TaskFullId(chatId, taskId);
        var result = await CompleteOrSkipTaskAsync(taskFullId, skip, client);

        if (!result.Success)
        {
            var response = request.CreateResponse(HttpStatusCode.BadRequest);

            if (result.Message is not null)
            {
                await response.WriteStringAsync(result.Message);
            }
            return response;
        }

        return request.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(DeleteTask))]
    public async Task<HttpResponseData> DeleteTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Delete, Route = "{chatId}/tasks/{taskId}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId)
    {
        var taskFullId = new TaskFullId(chatId, taskId);
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.DisableTask));
        return request.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(GetTask))]
    public async Task<HttpResponseData> GetTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "{chatId}/tasks/{taskId}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId)
    {
        var taskFullId = new TaskFullId(chatId, taskId);
        var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
        var task = await client.Entities.GetEntityAsync<MoneoTaskState>(entityId);

        if (task is null)
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(task.State.ToMoneoTaskDto());
        return response;
    }

    [Function(nameof(GetTasksListForChat))]
    public async Task<HttpResponseData> GetTasksListForChat(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "{chatId}/tasks")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId)
    {
        var tasks = await GetAllTasksForConversationAsync(chatId, client);

        if (tasks.Count == 0)
        {
            return request.CreateResponse(HttpStatusCode.NoContent);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(tasks);
        return response;
    }

    [Function(nameof(GetTasksList))]
    public async Task<HttpResponseData> GetTasksList(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "tasks")] HttpRequestData request,
               [DurableClient] DurableTaskClient client,
                      FunctionContext context)
    {
        var tasks = await GetAllTasksAsync(client);

        if (tasks.Count == 0)
        {
            return request.CreateResponse(HttpStatusCode.NoContent);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(tasks);
        return response;
    }

    [Function(nameof(MigrateTasks))]
    public async Task<HttpResponseData> MigrateTasks(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "migrate/tasks")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context)
    {
        var allTasks = await GetAllTasksAsync(client);

        if (allTasks.Count == 0)
        {
            return request.CreateResponse(HttpStatusCode.NoContent);
        }

        var succeeded = new List<string>();
        var failed = new List<string>();
        var skipped = new List<string>();


        foreach (var (id, taskManager) in allTasks)
        {
            if (!id.IsValidTaskFullId())
            {
                skipped.Add(id);
                continue;
            }

            try
            {
                var taskFullId = TaskFullId.CreateFromFullId(id);
                var entityId = new EntityInstanceId(nameof(MoneoTaskState), taskFullId.FullId);
                await client.Entities.SignalEntityAsync(entityId, nameof(TaskManager.PerformMigrationAction));
                succeeded.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate task: {Id}", id);
                failed.Add(id);
            }
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(allTasks);
        return response;
    }
}
