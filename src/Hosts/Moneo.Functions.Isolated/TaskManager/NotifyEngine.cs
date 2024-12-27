using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.BotRequests;

namespace Moneo.Functions.Isolated.TaskManager
{
    internal class NotifyEngine : INotifyEngine
    {
        private readonly ILogger<NotifyEngine> _logger;
        private readonly IChatAdapter _chatAdapter;

        public NotifyEngine(ILogger<NotifyEngine> logger, IChatAdapter chatAdapter)
        {
            _logger = logger;
            _chatAdapter = chatAdapter;
        }

        public async Task SendNotification(long chatId, string message)
        {
            await _chatAdapter.SendBotTextMessageAsync(new BotTextMessageRequest(chatId, message), default);
        }
    }
}
