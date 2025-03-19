using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

[UserCommand(CommandKey = "/createCron")]
public partial class CreateCronRequest : UserRequestBase
{
    public string TaskName { get; init; }
    public ChatState CalledFromState { get; init; }
    
    public CreateCronRequest(long conversationId, ChatState calledFromState, params string[] args) : base(conversationId, args)
    {
        CalledFromState = calledFromState;
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
    
    public CreateCronRequest(long conversationId, ChatState calledFromState, string taskName) : base(conversationId, taskName)
    {
        CalledFromState = calledFromState;
        TaskName = taskName;
    }
    
    public CreateCronRequest(long conversationId, params string[] args) : base(conversationId)
    {
        // the first arg should be the chat state
        CalledFromState = Enum.Parse<ChatState>(args[0], true);
        
        if (CalledFromState != ChatState.CreateTask && CalledFromState != ChatState.ChangeTask)
        {
            throw new ArgumentException("Invalid chat state for CRON creation");
        }
        
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
}

internal class CreateCronRequestHandler : IRequestHandler<CreateCronRequest, MoneoCommandResult>
{
    private readonly ICreateCronWorkflowManager _manager;

    public CreateCronRequestHandler(ICreateCronWorkflowManager manager)
    {
        _manager = manager;
    }
    
    public async Task<MoneoCommandResult> Handle(CreateCronRequest request, CancellationToken cancellationToken)
    {
        return await _manager.StartWorkflowAsync(request.ConversationId, request.CalledFromState);
    }
}