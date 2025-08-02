using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.Hosts.Chat.Api;
using Moneo.Hosts.Chat.Api.Tasks;
using Moneo.TaskManagement.Contracts;

namespace Moneo.TaskManagement.Workflows.CreateTask;

public interface ICreateTaskWorkflowManager : ICreateOrUpdateTaskWorkflowManager;

// TODO: Add some default Completed messages
[MoneoWorkflow]
public class CreateTaskWorkflowManager : ICreateTaskWorkflowManager
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreateTaskWorkflowManager> _logger;
    private readonly ITaskManagerClient _taskManagerClient;
    private readonly CreateOrUpdateTaskWorkflowManager _innerWorkflowManager;
    private readonly IWorkflowWithTaskDraftStateMachineRepository _chatStates;
    
    public CreateTaskWorkflowManager(
        IMediator mediator,
        ILogger<CreateTaskWorkflowManager> logger,
        IWorkflowWithTaskDraftStateMachineRepository chatStates,
        ITaskManagerClient taskManagerClient)
    {
        _mediator = mediator;
        _logger = logger;
        _taskManagerClient = taskManagerClient;
        _chatStates = chatStates;
        _innerWorkflowManager =
        new CreateOrUpdateTaskWorkflowManager(
            logger,
            mediator, // pass mediator as ISender
            CompleteWorkflowAsync);
        
        _innerWorkflowManager.SetResponse(TaskCreateOrUpdateState.End, new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Task created successfully!"
        });
    }
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string? taskName = null, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CreateTaskWorkflowStartedEvent(cmdContext.ConversationId, cmdContext.User?.Id ?? 0), cancellationToken);
        
        if (_chatStates.ContainsKey(cmdContext.GenerateConversationUserKey()))
        {
            // can't create a task while creating another task
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You cannot create a new task while you're still creating another one!"
            };
        }
        
        var machine = new TaskCreationStateMachine(cmdContext.ConversationId, cmdContext.User?.Id ?? 0, taskName);
        
        _chatStates.Add(cmdContext.GenerateConversationUserKey(), machine);

        return await _innerWorkflowManager.StartWorkflowAsync(machine, cancellationToken);
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default)
    {
        if (!_chatStates.TryGetValue(cmdContext.GenerateConversationUserKey(), out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You're not creating a task right now!"
            };
        }
        
        return await _innerWorkflowManager.ContinueWorkflowAsync(cmdContext, machine, userInput, cancellationToken);
    }

    public Task AbandonWorkflowAsync(long chatId)
    {
        throw new NotImplementedException();
    }
    
    private async Task CompleteWorkflowAsync(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine, CommandContext context)
    {
        var result = await _taskManagerClient.CreateTaskAsync(stateMachine.ConversationId, stateMachine.Draft.ToEditDto());
        
        if (!result.IsSuccess)
        {
            _logger.LogError(
                result.Exception,
                "Failed to create task for conversation {ConversationId}",
                stateMachine.ConversationId);
        }
        
        _chatStates.Remove(new ConversationUserKey(stateMachine.ConversationId, stateMachine.Draft.ForUserId));
        await _mediator.Send(new CreateTaskWorkflowCompletedEvent(stateMachine.ConversationId, context.User?.Id ?? 0));
    }
}
