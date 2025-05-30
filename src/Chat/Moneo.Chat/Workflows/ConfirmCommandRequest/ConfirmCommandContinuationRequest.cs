using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat;

[WorkflowContinuationCommand(nameof(ChatState.ConfirmCommand), "/continueConfirmCommand")]
public partial class ConfirmCommandContinuationRequest : UserRequestBase
{
    public string Text { get; }

    public ConfirmCommandContinuationRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        Text = string.Join(' ', args);
    }

    public ConfirmCommandContinuationRequest(long conversationId, ChatUser? user, string text) : base(conversationId, user, text)
    {
        Text = text;
    }
}

internal class ConfirmCommandContinuationRequestHandler : IRequestHandler<ConfirmCommandContinuationRequest, MoneoCommandResult>
{
    private readonly IConfirmCommandWorkflowManager _manager;

    public ConfirmCommandContinuationRequestHandler(IConfirmCommandWorkflowManager manager)
    {
        _manager = manager;
    }

    public Task<MoneoCommandResult> Handle(ConfirmCommandContinuationRequest request, CancellationToken cancellationToken)
        => _manager.ContinueWorkflowAsync(request.ConversationId, request.ForUserId, request.Text, cancellationToken);
}
