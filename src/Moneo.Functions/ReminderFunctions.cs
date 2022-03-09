using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Moneo.Functions
{
    public class ReminderFunctions
    {
        private readonly ILogger<ReminderFunctions> _logger;

        public ReminderFunctions(ILogger<ReminderFunctions> log)
        {
            _logger = log;
        }

        [FunctionName(nameof(DefuseTimer))]
        [OpenApiOperation(operationId: nameof(DefuseTimer), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        public async Task<HttpResponseMessage> DefuseTimer(
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
        [FunctionName(nameof(GetTimer))]
        [OpenApiOperation(operationId: nameof(DefuseTimer), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        public async Task<IActionResult> GetTimer(
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
    }
}

