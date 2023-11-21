using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

internal class ChitChatRequest : IRequest<MoneoCommandResult>
{
    public long ConversationId { get; init; }
    public const string Name = "ChitChat";
    public string UserText { get; init; }

    public ChitChatRequest(long conversationId, string userText)
    {
        ConversationId = conversationId;
        UserText = userText;
    }
}