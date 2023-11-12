using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.Workflows.Chitchat;

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