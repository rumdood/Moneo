using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.ChangeTask;

[WorkflowContinuationCommand(nameof(ChatState.ChangeTask), "/continueChange")]
public partial class ChangeTaskContinuationRequest: UserRequestBase
{
    public string Text { get; }

    public ChangeTaskContinuationRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        Text = string.Join(' ', args);
    }

    public ChangeTaskContinuationRequest(long conversationId, ChatUser? user, string text) : base(conversationId, user, text)
    {
        Text = text;
    }
}

internal class ChangeTaskContinuationRequestHandler(IChangeTaskWorkflowManager manager)
    : IRequestHandler<ChangeTaskContinuationRequest, MoneoCommandResult>
{
    public Task<MoneoCommandResult> Handle(ChangeTaskContinuationRequest request, CancellationToken cancellationToken)
        => manager.ContinueWorkflowAsync(request.ConversationId, request.ForUserId, request.Text, cancellationToken);
}

