using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Moneo.Models;
using Moneo.TaskManagement;

namespace Moneo.Functions
{
    public class TaskFunctions
    {
        private readonly ILogger<TaskFunctions> _logger;

        public TaskFunctions(ILogger<TaskFunctions> log)
        {
            _logger = log;
        }

        private static async Task<IDictionary<string, TaskManager>> GetAllTasks(IDurableEntityClient client)
        {
            var allTasks = new Dictionary<string, TaskManager>();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
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

                foreach (var entity in result.Entities)
                {
                    if (entity.State == null)
                    {
                        continue;
                    }

                    allTasks[entity.EntityId.EntityKey] = entity.State.ToObject<TaskManager>();
                }
            }
            while (query.ContinuationToken != null);

            return allTasks;
        }

        private async Task<IActionResult> CreateOrModifyTask(string taskId, IDurableEntityClient client, Action<ITaskManager> doWork)
        {
            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(entityId, r => doWork(r));

            return new OkResult();
        }

        private async Task<IActionResult> CompleteOrSkipTask(string taskId, bool isSkipped, IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return new BadRequestObjectResult("Task ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(entityId, r => r.MarkCompleted(isSkipped));
            _logger.LogInformation($"Reminder Defused for {taskId}");

            return new OkResult();
        }

        [FunctionName(nameof(CreateTask))]
        public async Task<IActionResult> CreateTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks/{taskId}")][FromBody] MoneoTaskDto task,
            string taskId,
            [DurableClient] IDurableEntityClient client) => await CreateOrModifyTask(taskId, client, r => r.InitializeTask(task));

        [FunctionName(nameof(UpdateTask))]
        public async Task<IActionResult> UpdateTask(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/{taskId}")][FromBody] MoneoTaskDto task,
            string taskId,
            [DurableClient] IDurableEntityClient client) => await CreateOrModifyTask(taskId, client, r => r.UpdateTask(task));


        [FunctionName(nameof(CompleteReminderTask))]
        public async Task<IActionResult> CompleteReminderTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks/{taskId}/{action}")]
            HttpRequest request,
            string taskId,
            string action,
            [DurableClient] IDurableEntityClient client) 
        {
            var skip = action switch
            {
                string v when v.Equals("complete", StringComparison.OrdinalIgnoreCase) => false,
                string v when v.Equals("skip", StringComparison.OrdinalIgnoreCase) => true,
                _ => throw new InvalidOperationException($"Unknown Action: {action}")
            };

            return await CompleteOrSkipTask(taskId, skip, client);
        }

        // have to use IActionResult because of issues with async and Kestrel
        [FunctionName(nameof(GetTaskStatus))]
        public async Task<IActionResult> GetTaskStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{taskId}")] HttpRequestMessage request,
            string taskId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return new BadRequestObjectResult("Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            var taskState = await client.ReadEntityStateAsync<TaskManager>(entityId);
            _logger.LogInformation($"Retrieved status for {taskId}");

            return new OkObjectResult(taskState);
        }

        [FunctionName(nameof(DeleteTask))]
        public async Task<HttpResponseMessage> DeleteTask(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/{taskId}")] HttpRequestMessage request,
            string taskId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "Task ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(nameof(TaskManager), x => x.DisableTask());
            _logger.LogInformation($"{taskId} has been deactivated");

            return request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [FunctionName(nameof(GetTasksList))]
        public async Task<IActionResult> GetTasksList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks")] HttpRequestMessage request,
            [DurableClient] IDurableEntityClient client)
        {
            var allTasks = await GetAllTasks(client);
            return new OkObjectResult(allTasks);
        }

        [FunctionName("CleanupDeactivated")]
        public async Task CleanupDeactivated(
            [TimerTrigger("%cleanup_cron%", RunOnStartup = false)] TimerInfo timer,
            [DurableClient] IDurableEntityClient client)
        {
            _logger.LogInformation($"Executing cleanup timer, next check at {timer.ScheduleStatus.Next}");

            var allTasks = await GetAllTasks(client);

            foreach (var (id, taskManager) in allTasks)
            {
                if (taskManager == null || taskManager.TaskState is { IsActive: true })
                {
                    continue;
                }

                try
                { 
                    await client.SignalEntityAsync<ITaskManager>(nameof(TaskManager), x => x.Delete());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete entity");
                }
            }

            await client.CleanEntityStorageAsync(true, true, CancellationToken.None);
        }
    }
}

