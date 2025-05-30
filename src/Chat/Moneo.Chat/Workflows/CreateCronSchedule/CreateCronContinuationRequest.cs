using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

[WorkflowContinuationCommand(nameof(ChatState.CreateCron), "/continueCron")]
public partial class CreateCronContinuationRequest : UserRequestBase
{
    public string Text { get; }
    
    public CreateCronContinuationRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        Text = string.Join(' ', args);
    }

    public CreateCronContinuationRequest(long conversationId, ChatUser? user, string text) : base(conversationId, user, text)
    {
        Text = text;
    }
}

internal class CreateCronContinuationRequestHandler : IRequestHandler<CreateCronContinuationRequest, MoneoCommandResult>
{
    private readonly ICreateCronWorkflowManager _manager;

    public CreateCronContinuationRequestHandler(ICreateCronWorkflowManager manager)
    {
        _manager = manager;
    }

    public Task<MoneoCommandResult> Handle(CreateCronContinuationRequest request,
        CancellationToken cancellationToken)
        => _manager.ContinueWorkflowAsync(request.ConversationId, request.ForUserId, request.Text);
}