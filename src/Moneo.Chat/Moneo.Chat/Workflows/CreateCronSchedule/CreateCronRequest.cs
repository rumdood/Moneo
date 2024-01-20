using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

[UserCommand("/createCron")]
public partial class CreateCronRequest : UserRequestBase
{
    public string TaskName { get; init; }
    
    public CreateCronRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
}

internal class CreateCronRequestHandler : IRequestHandler<CreateCronRequest, MoneoCommandResult>
{
    private readonly ICreateCronManager _manager;

    public CreateCronRequestHandler(ICreateCronManager manager)
    {
        _manager = manager;
    }
    
    public async Task<MoneoCommandResult> Handle(CreateCronRequest request, CancellationToken cancellationToken)
    {
        return await _manager.StartWorkflowAsync(request.ConversationId);
    }
}