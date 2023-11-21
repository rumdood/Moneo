namespace Moneo.Chat;

public class BotClientConfiguration
{
    public string Token { get; set; } = default!;
    public long MasterConversationId { get; set; }
    public string FunctionKey { get; set; } = default!;
    public string ApiBase { get; set; } = default!;
}