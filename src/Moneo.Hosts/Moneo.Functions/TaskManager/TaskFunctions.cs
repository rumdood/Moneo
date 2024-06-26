using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Models;

namespace Moneo.Functions;

internal static class HttpVerbs
{
    public const string Delete = "delete";
    public const string Get = "get";
    public const string Patch = "patch";
    public const string Post = "post";
    public const string Put = "put";
}

public class TaskFunctions
{
    private readonly ILogger<TaskFunctions> _logger;
    private readonly IMoneoTaskFactory _taskFactory;

    public TaskFunctions(IMoneoTaskFactory taskFactory, ILogger<TaskFunctions> log)
    {
        _taskFactory = taskFactory;
        _logger = log;
    }

    private async Task<Dictionary<string, TaskManager>> GetAllTasksAsync(IDurableEntityClient client)
    {
        using var tokenSource = new CancellationTokenSource();
        var cancelToken = tokenSource.Token;

        var query = new EntityQuery
        {
            FetchState = true,
            EntityName = nameof(TaskManager)
        };

        do
        {
            var result = await client.ListEntitiesAsync(query, cancelToken);

            if (result?.Entities != null && !(bool)result?.Entities.Any())
            {
                break;
            }

            var durableEntityStatusEnumerable = result!.Entities;

            if (durableEntityStatusEnumerable is null)
            {
                return new Dictionary<string, TaskManager>();
            }

            var results = new Dictionary<string, TaskManager>();

            foreach (var entity in durableEntityStatusEnumerable)
            {
                try
                {
                    var taskManager = entity.State.ToObject<TaskManager>();
                    _ = results.TryAdd(entity.EntityId.EntityKey, taskManager);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Failed to deserialize entity: {EntityId}", entity.EntityId.EntityKey);
                    continue;
                }
            }

            return results;
        }
        while (query.ContinuationToken != null);

        return new Dictionary<string, TaskManager>();
    }

    private async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksForConversationAsync(string chatId, IDurableEntityClient client)
    {
        var allTasks = await GetAllTasksAsync(client);
        var result = new Dictionary<string, MoneoTaskDto>();

        foreach (var kv in allTasks)
        {
            if (kv.Key.Contains('_'))
            {
                var id = TaskFullId.CreateFromFullId(kv.Key);
                if (id.ChatId.Equals(chatId))
                {
                    _ = result.TryAdd(id.TaskId, kv.Value.TaskState.ToMoneoTaskDto());
                }
            }
        }

        return result;
    }

    private async Task<IActionResult> CreateOrModifyTaskAsync(TaskFullId taskFullId, IDurableEntityClient client, Action<ITaskManager> doWork)
    {
        var entityId = new EntityId(nameof(TaskManager), taskFullId.FullId);
        await client.SignalEntityAsync(entityId, doWork);

        return new OkResult();
    }

    private async Task<IActionResult> CompleteOrSkipTaskAsync(TaskFullId taskFullId, bool isSkipped, IDurableEntityClient client)
    {
        if (taskFullId is null)
        {
            return new BadRequestObjectResult("Task ID Is Required");
        }

        var entityId = new EntityId(nameof(TaskManager), taskFullId.FullId);
        await client.SignalEntityAsync<ITaskManager>(entityId, r => r.MarkCompleted(isSkipped));
        _logger.LogInformation("Reminder Defused for {TaskId}", taskFullId.TaskId);

        return new OkResult();
    }

    private async Task<(List<string> Deleted, List<string> Failed)> DeleteInactiveTasksAsync(IDurableEntityClient client)
    {
        var allTasks = await GetAllTasksAsync(client);

        var deleted = new List<string>();
        var failed = new List<string>();

        foreach (var (id, taskManager) in allTasks)
        {
            if (taskManager.TaskState.IsActive)
            {
                continue;
            }

            try
            {
                var entityId = new EntityId(nameof(TaskManager), id);
                await client.SignalEntityAsync<ITaskManager>(entityId, r => r.Delete());
                deleted.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task: {Id}", id);
                failed.Add(id);
            }
        }

        return (Deleted: deleted, Failed: failed);
    }

    [OpenApiOperation(operationId: "MoneoCreateTask",
        tags: new[] { "CreateTask" },
        Summary = "Create new task",
        Description = "Will create a new MoneoTask and schedule any necessary reminders",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "taskId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the task to create",
        Description = "The ID of the task to create",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK,
        Summary = "If the create activity succeeded",
        Description = "If the create activity succeeded")]
    [FunctionName(nameof(CreateTaskAsync))]
    public async Task<IActionResult> CreateTaskAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "{chatId}/tasks/{taskId}")][FromBody] MoneoTaskDto task,
        long chatId,
        string taskId,
        [DurableClient] IDurableEntityClient client) =>
            await CreateOrModifyTaskAsync(
                new TaskFullId(chatId.ToString(), taskId), client,
                r => r.InitializeTask(task));

    [OpenApiOperation(operationId: "MoneoUpdateTask",
        tags: new[] { "UpdateTask" },
        Summary = "Updates an existing task",
        Description = "Will update an existing MoneoTask and schedule any necessary new reminders",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "taskId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the task to update",
        Description = "The ID of the task to update",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK,
        Summary = "If the update activity succeeded",
        Description = "If the update activity succeeded")]
    [FunctionName(nameof(UpdateTaskAsync))]
    public async Task<IActionResult> UpdateTaskAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Patch, Route = "{chatId}/tasks/{taskId}")][FromBody] MoneoTaskDto task,
        string chatId,
        string taskId,
        [DurableClient] IDurableEntityClient client) => await CreateOrModifyTaskAsync(new TaskFullId(chatId, taskId), client, r => r.UpdateTask(task));


    [OpenApiOperation(operationId: "CompleteMoneoTask",
        tags: new[] { "CompleteTask" },
        Summary = "Complete a task",
        Description = "Adds a new time for completion or skipping a task. Will defuse any pending due-date reminders",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "taskId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the task to complete",
        Description = "The ID of the task to complete",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "action",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The action (complete or skip) to perform on the task",
        Description = "The action (complete or skip) to perform on the task",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK,
        Summary = "If the completion/skip activity succeeded",
        Description = "If the completion/skip activity succeeded")]
    [FunctionName(nameof(CompleteReminderTaskAsync))]
    public async Task<IActionResult> CompleteReminderTaskAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "{chatId}/tasks/{taskId}/{action}")]
        HttpRequest request,
        string chatId,
        string taskId,
        string action,
        [DurableClient] IDurableEntityClient client)
    {
        var skip = action switch
        {
            not null when action.Equals("complete", StringComparison.OrdinalIgnoreCase) => false,
            not null when action.Equals("skip", StringComparison.OrdinalIgnoreCase) => true,
            _ => throw new InvalidOperationException($"Unknown Action: {action}")
        };

        return await CompleteOrSkipTaskAsync(new TaskFullId(chatId, taskId), skip, client);
    }

    [OpenApiOperation(operationId: "GetMoneoTask",
        tags: new[] { "GetTask" },
        Summary = "Gets a single MoneoTask",
        Description = "Gets the current data for a single MoneoTask",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "taskId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the task to retrieve",
        Description = "The ID of the task to retrieve",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(IMoneoTask),
        Summary = "The requested MoneoTask",
        Description = "The requested MoneoTask")]
    [FunctionName(nameof(GetTaskStatusAsync))]
    public async Task<ActionResult<IMoneoTaskDto>> GetTaskStatusAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "{chatId}/tasks/{taskId}")] HttpRequestMessage request,
        string chatId,
        string taskId,
        [DurableClient] IDurableEntityClient client)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return new BadRequestObjectResult("Reminder ID Is Required");
        }

        var fullId = new TaskFullId(chatId, taskId);
        var entityId = new EntityId(nameof(TaskManager), fullId.FullId);
        var taskState = await client.ReadEntityStateAsync<TaskManager>(entityId);

        if (!taskState.EntityExists)
        {
            return new NotFoundResult();
        }

        _logger.LogInformation("Retrieved status for {TaskId}", taskId);

        var dto = taskState.EntityState.TaskState.ToMoneoTaskDto();

        return new OkObjectResult(dto);
    }

    [OpenApiOperation(operationId: "DeactivateMoneoTask",
        tags: new[] { "DeactivateTask" },
        Summary = "Delete existing task",
        Description = "If the specified task exists, it will be deactivated and available for the cleanup routine to purge",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "taskId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the task to deactivate",
        Description = "The ID of the task to deactivate",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK,
        Summary = "If the deactivation succeeded",
        Description = "If the deactivation succeeded")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest,
        contentType: "application/text",
        bodyType: typeof(string),
        Summary = "If the request is missing the Task ID",
        Description = "If the request is missing the Task ID")]
    [FunctionName(nameof(DeactivateTask))]
    public async Task<HttpResponseMessage> DeactivateTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Delete, Route = "{chatId}/tasks/{taskId}")] HttpRequestMessage request,
        string chatId,
        string taskId,
        [DurableClient] IDurableEntityClient client)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return request.CreateResponse(HttpStatusCode.BadRequest, "Task ID Is Required");
        }

        var fullTaskId = new TaskFullId(chatId, taskId);
        var entityId = new EntityId(nameof(TaskManager), fullTaskId.FullId);

        await client.SignalEntityAsync<ITaskManager>(entityId, x => x.DisableTask());

        _logger.LogInformation("{TaskId} has been deactivated", taskId);

        return request.CreateResponse(HttpStatusCode.OK);
    }
    
    [OpenApiOperation(operationId: "GetAllMoneoTasksForChat",
        tags: new[] { "GetAllTasksForChat" },
        Summary = "A complete list of all tasks associated with a given chat",
        Description = "Returns a detailed list of MoneoTasks associated with a given chat",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "chatId",
        In = Microsoft.OpenApi.Models.ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "The ID of the conversation/user",
        Description = "The ID of the conversation/user",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(Dictionary<string, TaskManager>),
        Summary = "The list of tasks by Task ID",
        Description = "The list of tasks by Task ID")]
    [FunctionName(nameof(GetTasksListForChatAsync))]
    public async Task<ActionResult<Dictionary<string, TaskManager>>> GetTasksListForChatAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "{chatId}/tasks")] HttpRequestMessage request,
        string chatId,
        [DurableClient] IDurableEntityClient client)
    {
        var chatTasks = await GetAllTasksForConversationAsync(chatId, client);
        return new OkObjectResult(chatTasks);
    }

    [OpenApiOperation(operationId: "GetAllMoneoTasks",
        tags: new[] { "GetAllTasks" },
        Summary = "A complete list of all tasks in the system",
        Description = "Returns a detailed list of MoneoTasks in the system without any filters",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(Dictionary<string, TaskManager>),
        Summary = "The list of tasks by Task ID",
        Description = "The list of tasks by Task ID")]
    [FunctionName(nameof(GetTasksListAsync))]
    public async Task<ActionResult<Dictionary<string, TaskManager>>> GetTasksListAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, Route = "tasks")] HttpRequestMessage request,
        [DurableClient] IDurableEntityClient client)
    {
        var allTasks = await GetAllTasksAsync(client);
        return new OkObjectResult(allTasks);
    }

    [OpenApiOperation(operationId: "CleanupInactiveMoneoTasks",
        tags: new[] { "CleanupTasks" },
        Summary = "Cleans up existing tasks that are currently inactive",
        Description = "Cleans up existing tasks that are currently inactive and returns a list of tasks that have been deleted or that failed to be deleted",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof((List<string> Deleted, List<string> Failed)),
        Summary = "The list of successfully deleted tasks and the list of tasks that failed to be deleted",
        Description = "The list of successfully deleted tasks and the list of tasks that failed to be deleted")]
    [FunctionName(nameof(CleanupInactiveTasksViaHttp))]
    public async Task<ActionResult> CleanupInactiveTasksViaHttp(
               [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Delete, Route = "tasks/cleanup")] HttpRequestMessage request,
                      [DurableClient] IDurableEntityClient client)
    {
        var result = await DeleteInactiveTasksAsync(client);
        return new OkObjectResult(result);
    }

    [FunctionName(nameof(MigrateTasksAsync))]
    public async Task<IActionResult> MigrateTasksAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Patch, Route = "tasks/migrate")] HttpRequestMessage request,
        [DurableClient] IDurableEntityClient client)
    {
        var allTasks = await GetAllTasksAsync(client);

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
                var entityId = new EntityId(nameof(TaskManager), taskFullId.FullId);
                await client.SignalEntityAsync<ITaskManager>(entityId, r => r.PerformMigrationAction());
                succeeded.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate task: {Id}", id);
                failed.Add(id);
            }
        }

        return new OkObjectResult((Succeeded: succeeded, Failed: failed, Skipped: skipped));
    }
}
