namespace Moneo.Common;

public interface IBotClientConfiguration
{
    string BotToken { get; set; }
    long MasterConversationId { get; set; }
    string FunctionKey { get; set; }
    string CallbackToken {  get; set; }
    string TaskApiBase { get; set; }
    bool IsDetailedErrorsEnabled { get; set; }
    string ChatAdapter { get; set; }
    string DefaultTimezone { get; set; }
}