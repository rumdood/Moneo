using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Moneo.Functions.NotifyEngines
{
    public class TelegramNotify : INotifyEngine
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramNotify> _logger;
        private readonly long _chatId;

        public TelegramNotify(ILogger<TelegramNotify> logger)
        {
            var botToken = Environment.GetEnvironmentVariable("telegramBotToken", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Telegram Token Not Found");

            _botClient = new TelegramBotClient(botToken);
            _logger = logger;

            if (!long.TryParse(Environment.GetEnvironmentVariable("telegramChatId"), out var c))
            {
                _logger.LogError("Unable to determine chat ID");
                throw new ArgumentException("Telegram ChatID Not Found");
            }

            _chatId = c;
        }

        public async Task SendNotification(string message)
        {
            await _botClient.SendTextMessageAsync(_chatId, message);
        }
    }
}
