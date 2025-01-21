using Moneo.Core;

namespace Moneo.TelegramChat.Api.Configuration;

public class BotClientConfiguration : IBotClientConfiguration
{
    public string BotToken { get; set; }
    public long MasterConversationId { get; set; }
    public string FunctionKey { get; set; }
    public string MoneoApiKey { get; set; }
    public string CallbackToken { get; set; }
    public string TaskApiBase { get; set; }
    public bool IsDetailedErrorsEnabled { get; set; }
    public string ChatAdapter { get; set; }
}