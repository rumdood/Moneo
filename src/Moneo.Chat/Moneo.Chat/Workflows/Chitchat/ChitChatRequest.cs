using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.UserRequests;

namespace Moneo.Chat.Workflows.Chitchat;

[UserCommand("/chitchat")]
public partial class ChitChatRequest : UserRequestBase
{
    public string UserText { get; private set; }

    public ChitChatRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        UserText = string.Join(' ', args);
    }

    public ChitChatRequest(long conversationId, string userText) : base(conversationId, userText)
    {
        UserText = userText;
    }
}