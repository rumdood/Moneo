using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement.Models;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class MigrateTasksEndpoint : TaskManagerEndpointBase
{
    public MigrateTasksEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log) : base(tasksService, log)
    {
    }

    [Function(nameof(MigrateTasks))]
    public async Task<HttpResponseData> MigrateTasks(
        [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "migrate/tasks")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext context)
    {
        var allTasks = await _durableEntityTasksService.GetAllTasksDictionaryAsync(client);

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
