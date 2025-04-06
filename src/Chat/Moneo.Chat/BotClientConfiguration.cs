using Moneo.Common;

namespace Moneo.Chat;

public class BotClientConfiguration : IBotClientConfiguration
{
    public string BotToken { get; set; } = default!;
    public long MasterConversationId { get; set; }
    public string FunctionKey { get; set; } = default!;
    public string CallbackToken { get; set; } = default!;
    public string TaskApiBase { get; set; } = default!;
    public bool IsDetailedErrorsEnabled { get; set; }
    public string ChatAdapter { get; set; }
    public string DefaultTimezone { get; set; } = default!;
}