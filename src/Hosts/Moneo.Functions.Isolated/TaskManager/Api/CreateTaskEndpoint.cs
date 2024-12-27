using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.Obsolete.TaskManagement.Models;
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
        var result = await DurableEntityTasksService.CreateTaskAsync(taskFullId, client, task);

        if (result.Success)
        {
            return request.CreateResponse(HttpStatusCode.OK);
        }
        
        var response = request.CreateResponse(HttpStatusCode.BadRequest);

        if (!string.IsNullOrEmpty(result.Message))
        {
            await response.WriteStringAsync(result.Message);
        }

        return response;

    }
}
