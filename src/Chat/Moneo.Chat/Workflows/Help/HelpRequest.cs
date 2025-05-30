using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.Chitchat;

[UserCommand(CommandKey = "/help")]
public partial class HelpRequest : UserRequestBase
{
    public string UserText { get; private set; }

    public HelpRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        UserText = string.Join(' ', args);
    }

    public HelpRequest(long conversationId, ChatUser? user, string userText) : base(conversationId, user, userText)
    {
        UserText = userText;
    }
}

