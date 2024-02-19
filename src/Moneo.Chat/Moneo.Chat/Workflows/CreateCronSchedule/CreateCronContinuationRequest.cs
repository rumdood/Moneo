using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

[UserCommand(CommandKey = "/continueCron")]
public partial class CreateCronContinuationRequest : UserRequestBase
{
    public string Text { get; }
    
    public CreateCronContinuationRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        Text = string.Join(' ', args);
    }

    public CreateCronContinuationRequest(long conversationId, string text) : base(conversationId, text)
    {
        Text = text;
    }
}

internal class CreateCronContinuationRequestHandler : IRequestHandler<CreateCronContinuationRequest, MoneoCommandResult>
{
    private readonly ICreateCronManager _manager;

    public CreateCronContinuationRequestHandler(ICreateCronManager manager)
    {
        _manager = manager;
    }

    public Task<MoneoCommandResult> Handle(CreateCronContinuationRequest request,
        CancellationToken cancellationToken)
        => _manager.ContinueWorkflowAsync(request.ConversationId, request.Text);
}