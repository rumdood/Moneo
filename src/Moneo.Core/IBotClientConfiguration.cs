namespace Moneo.Core;

public interface IBotClientConfiguration
{
    string BotToken { get; set; }
    long MasterConversationId { get; set; }
    string FunctionKey { get; set; }
    string CallbackToken {  get; set; }
    string TaskApiBase { get; set; }
}