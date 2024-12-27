using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using System.Net;


namespace Moneo.Functions.Isolated.TaskManager;

internal class TaskOperationEndpoint : TaskManagerEndpointBase
{
    public TaskOperationEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskOperationEndpoint> log) : base(tasksService, log)
    {
    }
    
    [Function(nameof(PerformOperation))]
    public async Task<HttpResponseData> PerformOperation(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "{chatId}/tasks/{taskId}/{operation}")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string chatId,
        string taskId,
        string operation)
    {
        var taskFullId = new TaskFullId(chatId, taskId);

        var result = operation switch
        {
            not null when operation.Equals("complete", StringComparison.OrdinalIgnoreCase) =>
                await DurableEntityTasksService.CompleteOrSkipTaskAsync(taskFullId, false, client),
            not null when operation.Equals("skip", StringComparison.OrdinalIgnoreCase) => await DurableEntityTasksService
                .CompleteOrSkipTaskAsync(taskFullId, true, client),
            not null when operation.Equals("deactivate", StringComparison.OrdinalIgnoreCase) => await DurableEntityTasksService
                .DeactivateTaskAsync(taskFullId, client),
            not null when operation.Equals("delete", StringComparison.OrdinalIgnoreCase) => await DurableEntityTasksService
                .DeleteTaskAsync(taskFullId, client),
            _ => throw new InvalidOperationException($"Unknown Action: {operation}")
        };

        if (result.Success)
        {
            return request.CreateResponse(HttpStatusCode.OK);
        }
        
        var response = request.CreateResponse(HttpStatusCode.BadRequest);

        if (result.Message is not null)
        {
            await response.WriteStringAsync(result.Message);
        }
        
        return response;
    }
}