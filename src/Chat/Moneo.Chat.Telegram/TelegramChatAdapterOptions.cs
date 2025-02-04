using Moneo.Chat.ServiceCollectionExtensions;

namespace Moneo.Chat.Telegram;

public class TelegramChatAdapterOptions : ChatAdapterOptions
{
    public string BotToken { get; set; } = string.Empty;
    public long MasterConversationId { get; set; }
    public string CallbackToken { get; set; } = string.Empty;
}