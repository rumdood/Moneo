using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.ChangeTask;

[UserCommand(CommandKey = "/continueChange")]
public partial class ChangeTaskContinuationRequest: UserRequestBase
{
    public string Text { get; }
    
    public ChangeTaskContinuationRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        Text = string.Join(' ', args);
    }
    
    public ChangeTaskContinuationRequest(long conversationId, string text) : base(conversationId, text)
    {
        Text = text;
    }
}

internal class ChangeTaskContinuationRequestHandler(IChangeTaskWorkflowManager manager)
    : IRequestHandler<ChangeTaskContinuationRequest, MoneoCommandResult>
{
    public Task<MoneoCommandResult> Handle(ChangeTaskContinuationRequest request, CancellationToken cancellationToken)
        => manager.ContinueWorkflowAsync(request.ConversationId, request.Text);
}