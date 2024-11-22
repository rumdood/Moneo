using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class UpdateTaskEndpoint : TaskManagerEndpointBase
{
    public UpdateTaskEndpoint(IDurableEntityTasksService tasksService, ILogger<UpdateTaskEndpoint> log) : base(tasksService, log)
    {
    }

    [Function(nameof(UpdateTask))]
    public async Task<HttpResponseData> UpdateTask(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Put, Route = "{chatId}/tasks/{taskId}")]
        HttpRequestData request,
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
}
