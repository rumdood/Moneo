using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.Chitchat;

[UserCommand(CommandKey = "/chitchat")]
public partial class ChitChatRequest : UserRequestBase
{
    public string UserText { get; private set; }

    public ChitChatRequest(CommandContext context) : base(context)
    {
        UserText = string.Join(' ', context.Args);
    }

    public ChitChatRequest(long conversationId, ChatUser? user, string userText) : base(conversationId, user, userText)
    {
        UserText = userText;
    }
}
