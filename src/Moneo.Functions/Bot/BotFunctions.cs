using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moneo.Functions.Bot;
using Telegram.Bot;

namespace Moneo.Functions
{
    public class BotFunctions
    {
        private readonly ILogger<BotFunctions> _logger;
        private readonly ITelegramBotClient _botClient;
        private readonly BotService _botService;

        public BotFunctions(ILogger<BotFunctions> log, ITelegramBotClient botClient, BotService botService)
        {
            _logger = log;
            _botClient = botClient;
            _botService = botService;
        }

        [FunctionName(nameof(SetupBot))]
        public async Task<IActionResult> SetupBot(
            [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Get, HttpVerbs.Post, Route = null)] HttpRequestMessage request)
        {
            var handleCommandUrl = request.RequestUri.AbsoluteUri.Replace(nameof(SetupBot), nameof(HandleUserCommand), ignoreCase: true, culture: System.Globalization.CultureInfo.InvariantCulture);
            await _botClient.SetWebhookAsync(handleCommandUrl);
            _logger.LogInformation("Bot Webhook Created");
            return new OkObjectResult("Bot Webhook Created");
        }

        [FunctionName(nameof(HandleUserCommand))]
        public async Task<IActionResult> HandleUserCommand(
            [HttpTrigger(AuthorizationLevel.Function, HttpVerbs.Post, Route = "usercommand")] HttpRequestMessage request,
            [DurableClient] IDurableClient client)
        {
            var cmdString = await request.Content.ReadAsStringAsync();
            var update = BotService.GetTelegramUpdateFromJson(cmdString);

            if (update.Message is null) 
            { 
                return new NoContentResult(); 
            }

            await _botService.SendWaiting(update.Message.From.Id);

            if (update.Message.ReplyToMessage is not null)
            {
                var reply = update.Message.ReplyToMessage;
                var reference = "need to build the reference";

                if (reference is null)
                {
                    _logger.LogError("Message not found: {0}", update.Message.ReplyToMessage);
                    return new NotFoundObjectResult("Message not found");
                }
            }

            return new OkResult();
        }
    }
}

