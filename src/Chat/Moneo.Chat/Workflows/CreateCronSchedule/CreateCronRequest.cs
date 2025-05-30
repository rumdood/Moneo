using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

[UserCommand(CommandKey = "/createCron")]
public partial class CreateCronRequest : UserRequestBase
{
    public string TaskName { get; init; }
    public ChatState CalledFromState { get; init; }
    
    public CreateCronRequest(long conversationId, ChatUser? user, ChatState calledFromState, params string[] args) : base(conversationId, user, args)
    {
        CalledFromState = calledFromState;
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
    
    public CreateCronRequest(long conversationId, ChatUser? user, ChatState calledFromState, string taskName) : base(conversationId, user, taskName)
    {
        CalledFromState = calledFromState;
        TaskName = taskName;
    }
    
    public CreateCronRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user)
    {
        // the first arg should be the chat state
        CalledFromState = ChatState.FromName(args[0]);
        
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
        return await _manager.StartWorkflowAsync(request.ConversationId, request.ForUserId, request.CalledFromState, cancellationToken);
    }
}