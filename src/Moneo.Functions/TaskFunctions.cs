using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Dynamitey;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Moneo.Functions
{
    public class TaskFunctions
    {
        private readonly ILogger<TaskFunctions> _logger;
        private readonly INotifyEngine _notifier;

        public TaskFunctions(INotifyEngine notifier,
            ILogger<TaskFunctions> log)
        {
            _notifier = notifier;
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

        private async Task<IActionResult> UpdateMoneoTask(string taskId, MoneoTask task, IDurableEntityClient client, Action<ITaskManager> doWork)
        {
            if (task == null || string.IsNullOrEmpty(taskId))
            {
                return new BadRequestResult();
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(entityId, r => doWork(r));

            return new OkObjectResult(task);
        }

        private async Task<IActionResult> DefuseTaskReminder(string taskId, bool skip, IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return new BadRequestObjectResult("Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(entityId, r => r.MarkCompleted(false));
            _logger.LogInformation($"Reminder Defused for {taskId}");

            return new OkResult();
        }

        [FunctionName(nameof(CreateTask))]
        public async Task<IActionResult> CreateTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks/{taskId}")][FromBody] MoneoTask task,
            string taskId,
            [DurableClient] IDurableEntityClient client) => await UpdateMoneoTask(taskId, task, client, r => r.InitializeTask(task));

        [FunctionName(nameof(UpdateTask))]
        public async Task<IActionResult> UpdateTask(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/{taskId}")][FromBody] MoneoTask task,
            string taskId,
            [DurableClient] IDurableEntityClient client) => await UpdateMoneoTask(taskId, task, client, r => r.UpdateTask(task));

        [FunctionName(nameof(TestTask))]
        public async Task<IActionResult> TestTask(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/{taskId}/test")][FromBody] MoneoTask task,
            string taskId,
            [DurableClient] IDurableEntityClient client)
        {
            return await UpdateMoneoTask(taskId, task, client, r => r.UpdateTask(task));
        }


        [FunctionName(nameof(CompleteReminderTask))]
        public async Task<IActionResult> CompleteReminderTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks/{taskId}/{action}")]
            string taskId,
            string action,
            [DurableClient] IDurableEntityClient client) 
        {
            var skip = action switch
            {
                string v when v.Equals("defuse", StringComparison.OrdinalIgnoreCase) => false,
                string v when v.Equals("complete", StringComparison.OrdinalIgnoreCase) => false,
                string v when v.Equals("skip", StringComparison.OrdinalIgnoreCase) => true,
                _ => throw new InvalidOperationException($"Unknown Action: {action}")
            };

            return await DefuseTaskReminder(taskId, skip, client);
        }

        // have to use IActionResult because of issues with async and Kestrel
        [FunctionName(nameof(GetReminderStatus))]
        public async Task<IActionResult> GetReminderStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{taskId}")] HttpRequestMessage request,
            string taskId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return new BadRequestObjectResult("Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            var reminderState = await client.ReadEntityStateAsync<TaskManager>(entityId);
            _logger.LogInformation($"Retrieved status for {taskId}");

            return new OkObjectResult(reminderState.EntityState);
        }

        [FunctionName(nameof(DeleteReminder))]
        public async Task<HttpResponseMessage> DeleteReminder(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/{taskId}")] HttpRequestMessage request,
            string taskId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(TaskManager), taskId);
            await client.SignalEntityAsync<ITaskManager>(nameof(TaskManager), x => x.DisableTask());
            _logger.LogInformation($"{taskId} has been deactivated");

            return request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [FunctionName(nameof(GetRemindersList))]
        public async Task<IActionResult> GetRemindersList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reminders")] HttpRequestMessage request,
            [DurableClient] IDurableEntityClient client)
        {
            var allReminders = await GetAllTasks(client);
            return new OkObjectResult(allReminders);
        }

        [FunctionName("CheckReminders")]
        public async Task CheckReminders(
            [TimerTrigger("%check_reminder_cron%", RunOnStartup = false)]  TimerInfo timer,
            [DurableClient] IDurableEntityClient client)
        {
            _logger.LogInformation($"Executing check timer, next check at {timer.ScheduleStatus.Next}");

            if (!int.TryParse(Environment.GetEnvironmentVariable("defuseThresholdHours"), out var threshold))
            {
                _logger.LogError("Unable to retreive defuse threshold, will use 1 hour");
                threshold = 1;
            }

            var allReminders = await GetAllTasks(client);

            foreach (var (id, reminder) in allReminders)
            {
                if (reminder == null || (reminder.LastDefused.HasValue && DateTime.UtcNow.Subtract(reminder.LastDefused.Value).TotalHours <= threshold))
                {
                    continue;
                }

                _logger.LogInformation($"Send a reminder for {id}");
                await client.SignalEntityAsync<ITaskManager>(new EntityId(nameof(TaskManager), id), x => x.CheckSendReminder());
            }
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
                if (taskManager == null || taskManager.IsActive)
                {
                    continue;
                }

                await client.SignalEntityAsync<ITaskManager>(nameof(TaskManager), x => x.Delete());
            }
        }
    }
}

