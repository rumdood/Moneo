using MediatR;
using Moneo.Bot.Commands;
using Moneo.Bot.UserRequests;

namespace Moneo.Bot.Workflows.Chitchat;

internal class ChitChatRequest : IUserRequest, IRequest<MoneoCommandResult>
{
    public long ConversationId { get; init; }
    public string Name => "ChitChat";
    public string UserText { get; init; }

    public ChitChatRequest(long conversationId, string userText)
    {
        ConversationId = conversationId;
        UserText = userText;
    }
}