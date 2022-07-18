using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Moneo.Functions
{
    public class ReminderFunctions
    {
        private readonly ILogger<ReminderFunctions> _logger;
        private readonly INotifyEngine _notifier;

        public ReminderFunctions(INotifyEngine notifier,
            ILogger<ReminderFunctions> log)
        {
            _notifier = notifier;
            _logger = log;
        }

        private static async Task<IDictionary<string, ReminderState>> GetAllReminders(IDurableEntityClient client)
        {
            var allReminders = new Dictionary<string, ReminderState>();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            var query = new EntityQuery
            {
                FetchState = true,
                EntityName = nameof(ReminderState)
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

                    allReminders[entity.EntityId.EntityKey] = entity.State.ToObject<ReminderState>();
                }
            }
            while (query.ContinuationToken != null);

            return allReminders;
        }


        [FunctionName(nameof(DefuseReminder))]
        public async Task<HttpResponseMessage> DefuseReminder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reminders/{reminderId}")] HttpRequestMessage request,
            string reminderId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(reminderId))
            {
                return request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(ReminderState), reminderId);
            await client.SignalEntityAsync<IReminderState>(entityId, r => r.Defuse());
            _logger.LogInformation($"Reminder Defused for {reminderId}");

            return request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        // have to use IActionResult because of issues with async and Kestrel
        [FunctionName(nameof(GetReminderStatus))]
        public async Task<IActionResult> GetReminderStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reminders/{reminderId}")] HttpRequestMessage request,
            string reminderId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(reminderId))
            {
                return new BadRequestObjectResult("Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(ReminderState), reminderId);
            var reminderState = await client.ReadEntityStateAsync<ReminderState>(entityId);
            _logger.LogInformation($"Retrieved status for {reminderId}");

            return new OkObjectResult(reminderState.EntityState);
        }

        [FunctionName(nameof(DeleteReminder))]
        public async Task<HttpResponseMessage> DeleteReminder(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "reminders/{reminderId}")] HttpRequestMessage request,
            string reminderId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(reminderId))
            {
                return request.CreateResponse(System.Net.HttpStatusCode.BadRequest, "Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(ReminderState), reminderId);
            await client.SignalEntityAsync<IReminderState>(nameof(ReminderState), x => x.Delete());
            _logger.LogInformation($"Reminder Deleted for {reminderId}");

            return request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [FunctionName(nameof(GetRemindersList))]
        public async Task<IActionResult> GetRemindersList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reminders")] HttpRequestMessage request,
            [DurableClient] IDurableEntityClient client)
        {
            var allReminders = await GetAllReminders(client);
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

            var allReminders = await GetAllReminders(client);

            foreach (var (id, reminder) in allReminders)
            {
                if (reminder == null || DateTime.UtcNow.Subtract(reminder.LastDefused).TotalHours <= threshold)
                {
                    continue;
                }

                _logger.LogInformation($"Send a reminder for {id}");
                await client.SignalEntityAsync<IReminderState>(new EntityId(nameof(ReminderState), id), x => x.CheckSendReminder());
            }
        }
    }
}

