namespace Moneo.Chat.Workflows.Chitchat;

[UserCommand(CommandKey = "/help")]
public partial class HelpRequest : UserRequestBase
{
    public string UserText { get; private set; }

    public HelpRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        UserText = string.Join(' ', args);
    }

    public HelpRequest(long conversationId, string userText) : base(conversationId, userText)
    {
        UserText = userText;
    }
}