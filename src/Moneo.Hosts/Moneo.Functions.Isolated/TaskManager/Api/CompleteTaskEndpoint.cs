using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class CompleteTaskEndpoint : TaskManagerEndpointBase
{
    public CompleteTaskEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log) : base(tasksService, log)
    {
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
        var result = await _durableEntityTasksService.CompleteOrSkipTaskAsync(taskFullId, skip, client);

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
}
