using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moneo.TaskManagement;
using System.Threading.Tasks;

namespace Moneo.Functions.Bot
{
    internal class ActivityFunctions
    {
        private readonly ILogger<ActivityFunctions> _logger;
        private readonly BotService _botService;

        public ActivityFunctions(ILogger<ActivityFunctions> logger, BotService botService)
        {
            _logger = logger;
            _botService = botService;
        }

        internal async Task<BotResponse> ActivityCompleteMoneoTask(
            [ActivityTrigger] BotCommand command,
            [DurableClient] IDurableEntityClient client)
        {
            _logger.LogTrace("Peforming Complete Task Activity");
            var entityId = new EntityId(nameof(Conversation), command.ChatId.ToString());
            var conversationState = await client.ReadEntityStateAsync<Conversation>(entityId);
            var taskToComplete = command.Argument;

            var taskEntityId = new EntityId(nameof(TaskManager), taskToComplete);
            await client.SignalEntityAsync<ITaskManager>(taskEntityId, r => r.MarkCompleted(false));

            return new BotResponse("");
        }
    }
}
