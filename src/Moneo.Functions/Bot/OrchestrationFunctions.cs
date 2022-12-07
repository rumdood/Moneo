using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Moneo.Functions.Bot
{
    public class OrchestrationFunctions
    {
        private readonly ILogger<OrchestrationFunctions> _logger;
        private readonly ContextFactory _contextFactory;
        private readonly BotService _botService;

        public OrchestrationFunctions(ILogger<OrchestrationFunctions> logger,
            ContextFactory contextFactory,
            BotService botService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _botService = botService;
        }

        [FunctionName(nameof(RunOrchestrator))]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext ctx)
        {
            var message = ctx.GetInput<Message>();

            var conversationId = new EntityId(nameof(Conversation), message.From.Id.ToString());
            var proxy = ctx.CreateEntityProxy<IConversation>(conversationId);
            var state = await proxy.GetState();
            var conversationContext = _contextFactory.RestoreContext(state);

            var (response, activity) = conversationContext.GetAction(message.Text);
            proxy.SetState(conversationContext.CurrentState.Type);

            // pre-execute
            if (response != default)
            {
                await ctx.CallActivityAsync(nameof(SendResponse), response with { ChatId = message.From.Id});
            }

            if (!string.IsNullOrEmpty(activity))
            {
                return;
            }

            // execute
            var postActivityResponse = await ctx.CallActivityAsync<BotResponse>(activity, message);

            if (postActivityResponse != default)
            {
                // post-execute response
                await ctx.CallActivityAsync(nameof(SendResponse), postActivityResponse with { ChatId = message.From.Id });
            }
        }

        [FunctionName(nameof(SendResponse))]
        public async Task SendResponse([ActivityTrigger] BotResponse botResponse)
        {
            try
            {
                await _botService.SendResponse(botResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Sending Response", ex);
            }
        }
    }
}
