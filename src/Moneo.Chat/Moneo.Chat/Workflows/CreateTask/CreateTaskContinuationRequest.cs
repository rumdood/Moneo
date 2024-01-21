using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.UserRequests;

namespace Moneo.Chat.Workflows.CreateTask;

[UserCommand("/continueCreate")]
public partial class CreateTaskContinuationRequest : UserRequestBase
{
    public string Text { get; }

    public CreateTaskContinuationRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        Text = string.Join(' ', args);
    }

    public CreateTaskContinuationRequest(long conversationId, string text) : base(conversationId, text)
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
        => _manager.ContinueWorkflowAsync(request.ConversationId, request.Text);
}
