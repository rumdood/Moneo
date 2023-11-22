namespace Moneo.Core;

public interface IBotClientConfiguration
{
    string Token { get; set; }
    long MasterConversationId { get; set; }
    string FunctionKey { get; set; }
    string ApiBase { get; set; }
}