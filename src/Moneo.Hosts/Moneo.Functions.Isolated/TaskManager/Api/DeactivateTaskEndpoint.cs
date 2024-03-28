using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class DeactivateTaskEndpoint : TaskManagerEndpointBase
{
    public DeactivateTaskEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log) : base(tasksService, log)
    {
    }

    [Function(nameof(DeactivateTask))]
    public async Task<HttpResponseData> DeactivateTask(
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
}
