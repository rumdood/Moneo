using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.Models;
using Telegram.Bot;

namespace Moneo.Functions.Chat;

public class ChatFunctions
{
    private readonly ILogger<ChatFunctions> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConversationManager _conversationManager;

    public ChatFunctions(ILogger<ChatFunctions> logger, IConversationManager conversationManager)
    {
        _logger = logger;
        _conversationManager = conversationManager;
    }

    public async Task<IActionResult> SetupAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest request)
    {
        var callbackUrl = request.Path.Value.Replace(nameof(SetupAsync), nameof(ReceiveUpdateAsync), StringComparison.OrdinalIgnoreCase);
        await _botClient.SetWebhookAsync(callbackUrl);
        return new OkResult();
    }
    
    [FunctionName(nameof(ReceiveUpdateAsync))]
    public async Task<IActionResult> ReceiveUpdateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)][FromBody] Telegram.Bot.Types.Update update)
    {
        if (update.Message is { } message && !string.IsNullOrEmpty(message.Text))
        {
            try
            {
                await _conversationManager.ProcessUserMessageAsync(new UserMessage(message.Chat.Id, message.Text,
                    message.Chat.FirstName ?? message.Chat.Username!, message.Chat.LastName));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An Error Occurred");
                return new ExceptionResult(e, false);
            }
        }

        return new OkResult();
    }
}
