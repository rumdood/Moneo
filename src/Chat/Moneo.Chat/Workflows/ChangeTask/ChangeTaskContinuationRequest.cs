using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.ChangeTask;

[WorkflowContinuationCommand(nameof(ChatState.ChangeTask), "/continueChange")]
public partial class ChangeTaskContinuationRequest: UserRequestBase
{
    public string Text { get; }

    public ChangeTaskContinuationRequest(CommandContext context) : base(context)
    {
        Text = string.Join(' ', context.Args);
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
        => manager.ContinueWorkflowAsync(request.Context, request.Text, cancellationToken);
}

