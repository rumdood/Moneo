using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class GetTaskEndpoints : TaskManagerEndpointBase
{
    public GetTaskEndpoints(IDurableEntityTasksService tasksService, ILogger<GetTaskEndpoints> log) : base(tasksService, log)
    {
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
        var tasks = await DurableEntityTasksService.GetAllTasksDictionaryForConversationAsync(chatId, client);

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
        var tasks = await DurableEntityTasksService.GetAllTasksDictionaryAsync(client);

        if (tasks.Count == 0)
        {
            return request.CreateResponse(HttpStatusCode.NoContent);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(tasks);
        return response;
    }
}
