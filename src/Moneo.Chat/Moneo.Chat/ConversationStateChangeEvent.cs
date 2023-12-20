using MediatR;

namespace Moneo.Chat;

public record ConversationStateChangeEvent(long TargetConversation, ConversationState TargetState) : IRequest;

internal class ConversationStateChangeEventHandler : IRequestHandler<ConversationStateChangeEvent>
{
    private readonly IConversationManager _conversationManager;

    public ConversationStateChangeEventHandler(IConversationManager conversationManager)
    {
        _conversationManager = conversationManager;
    }
    
    public Task Handle(ConversationStateChangeEvent request, CancellationToken cancellationToken)
    {
        _conversationManager.SetConversationState(request.TargetConversation, request.TargetState);
        return Task.CompletedTask;
    }
}