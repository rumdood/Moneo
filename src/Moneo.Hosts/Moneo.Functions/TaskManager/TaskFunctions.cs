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

    private static async Task<Dictionary<string, TaskManager>> GetAllTasksAsync(IDurableEntityClient client)
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

            if (!(bool)result?.Entities.Any())
            {
                break;
            }

            return result!.Entities.Where(x => x.State is not null)
                .ToDictionary(x => x.EntityId.EntityKey, x => x.State.ToObject<TaskManager>());
        }
        while (query.ContinuationToken != null);

        return new Dictionary<string, TaskManager>();
    }

    private static async Task<Dictionary<string, MoneoTaskDto>> GetAllTasksForConversationAsync(string chatId, IDurableEntityClient client)
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

            if (!(bool)result?.Entities.Any())
            {
                break;
            }

            return result!.Entities
                .Where(x => x.EntityId.EntityKey.Contains('_')) // since there's no way to delete the old ones...
                .Select(x => new
                {
                    FullId = TaskFullId.CreateFromFullId(x.EntityId.EntityKey), 
                    x.State
                })
                .Where(x => x.State is not null && x.FullId.ChatId.Equals(chatId))
                .ToDictionary(x => x.FullId.TaskId, x => x.State.ToObject<TaskManager>().TaskState.ToMoneoTaskDto());
        }
        while (query.ContinuationToken != null);

        return new Dictionary<string, MoneoTaskDto>();
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
    [FunctionName(nameof(DeleteTaskAsync))]
    public async Task<HttpResponseMessage> DeleteTaskAsync(
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

    [FunctionName(nameof(MigrateTasksAsync))]
    public async Task<IActionResult> MigrateTasksAsync(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Patch, Route = "tasks/migrate")] HttpRequestMessage request,
        [DurableClient] IDurableEntityClient client)
    {
        var allTasks = await GetAllTasksAsync(client);

        var succeeded = new List<string>();
        var failed = new List<string>();

        foreach (var (id, taskManager) in allTasks)
        {
            if (taskManager is not { TaskState: var taskState } || taskState is not { IsActive: true } || taskManager.ChatId > 0)
            {
                continue;
            }

            try
            {
                var state = taskManager.TaskState;
                var stateDto = _taskFactory.CreateTaskDto(state);

                var newId = new TaskFullId(MoneoConfiguration.LegacyChatId.ToString(), id);
                var newEntityId = new EntityId(nameof(TaskManager), newId.FullId);

                await client.SignalEntityAsync<ITaskManager>(new EntityId(nameof(TaskManager), id), x => x.DisableTask());

                await client.SignalEntityAsync<ITaskManager>(
                    newEntityId, x => x.InitializeTask(stateDto));
                succeeded.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate task: {Id}", id);
                failed.Add(id);
            }
        }

        return new OkObjectResult((succeeded, failed));
    }
}