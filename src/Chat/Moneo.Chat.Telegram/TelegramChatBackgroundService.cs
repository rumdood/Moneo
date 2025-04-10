using Microsoft.Extensions.Hosting;
using Moneo.Chat.BotRequests;
using Telegram.Bot.Types;

namespace Moneo.Chat.Telegram;

public class TelegramChatBackgroundService : IHostedService
{
    private readonly IChatAdapter _chatAdapter;
    
    public Task StartAsync(CancellationToken cancellationToken) => _chatAdapter.StartReceivingAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _chatAdapter.StopReceivingAsync(cancellationToken);

    public TelegramChatBackgroundService(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
}