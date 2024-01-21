using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Moneo.Functions.NotifyEngines
{
    public class TelegramNotify : INotifyEngine
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramNotify> _logger;

        public TelegramNotify(ILogger<TelegramNotify> logger)
        {
            var botToken = MoneoConfiguration.TelegramBotToken;

            _botClient = new TelegramBotClient(botToken);
            _logger = logger;
        }

        public async Task SendNotification(long chatId, string message)
        {
            _logger.LogDebug("SendNotification: {0}", new { chatId, message });
            await _botClient.SendTextMessageAsync(chatId, message);
        }
    }
}
