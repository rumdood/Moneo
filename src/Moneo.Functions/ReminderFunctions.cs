using System;
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

        public ReminderFunctions(ILogger<ReminderFunctions> log)
        {
            _logger = log;
        }

        [FunctionName(nameof(DefuseReminder))]
        public async Task<HttpResponseMessage> DefuseReminder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reminders/{reminderId}")] HttpRequestMessage request,
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reminders/{reminderId}")] HttpRequestMessage request,
            string reminderId,
            [DurableClient] IDurableEntityClient client)
        {
            if (string.IsNullOrEmpty(reminderId))
            {
                return new BadRequestObjectResult("Reminder ID Is Required");
            }

            var entityId = new EntityId(nameof(ReminderState), reminderId);
            var reminderState = await client.ReadEntityStateAsync<ReminderState>(entityId);
            _logger.LogInformation($"Reminder Defused for {reminderId}");

            return new OkObjectResult(reminderState.EntityState);
        }

        [FunctionName("CheckReminders")]
        public async Task CheckReminders(
            [TimerTrigger("0 0/5 * * * *", RunOnStartup = true)]  TimerInfo timer,
            [DurableClient] IDurableEntityClient client)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            var query = new EntityQuery
            {
                PageSize = 10,
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

                    var reminder = entity.State.ToObject<ReminderState>();

                    if (reminder == null || DateTime.UtcNow.Subtract(reminder.LastTaken).TotalHours < 8)
                    {
                        continue;
                    }

                    _logger.LogInformation("Send a reminder");
                }
            } 
            while (query.ContinuationToken != null);
        }
    }
}

