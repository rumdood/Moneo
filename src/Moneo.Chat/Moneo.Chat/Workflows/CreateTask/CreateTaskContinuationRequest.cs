using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.UserRequests;

namespace Moneo.Chat.Workflows.CreateTask;

public class CreateTaskContinuationRequest : IRequest<MoneoCommandResult>, IUserRequest
{
    public long ConversationId { get; init; }
    public string Name => "CreateContinue";
    public string Text { get; }

    public CreateTaskContinuationRequest(long conversationId, string text)
    {
        ConversationId = conversationId;
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