using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class CreateTaskEndpoint : TaskManagerEndpointBase
{
    public CreateTaskEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log) : base(tasksService, log)
    {
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
}
