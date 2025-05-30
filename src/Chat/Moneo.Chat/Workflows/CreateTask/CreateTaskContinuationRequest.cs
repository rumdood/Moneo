using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.CreateTask;

[WorkflowContinuationCommand(nameof(ChatState.CreateTask), "/continueCreateTask")]
public partial class CreateTaskContinuationRequest : UserRequestBase
{
    public string Text { get; }

    public CreateTaskContinuationRequest(CommandContext context) : base(context)
    {
        Text = string.Join(' ', context.Args);
    }

    public CreateTaskContinuationRequest(long conversationId, ChatUser? user, string text) : base(conversationId, user, text)
    {
        Text = text;
    }
}

internal class CreateTaskContinuationRequestHandler : IRequestHandler<CreateTaskContinuationRequest, MoneoCommandResult>
{
    private readonly ICreateTaskWorkflowManager _manager;

    public CreateTaskContinuationRequestHandler(ICreateTaskWorkflowManager manager)
    {
        _manager = manager;
    }
    public Task<MoneoCommandResult> Handle(CreateTaskContinuationRequest request, CancellationToken cancellationToken)
        => _manager.ContinueWorkflowAsync(request.Context, request.Text, cancellationToken);
}
