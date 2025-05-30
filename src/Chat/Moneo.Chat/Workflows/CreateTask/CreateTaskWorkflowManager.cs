using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Chat.Workflows.CreateTask;

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
            stateMachine => mediator.Send(new CreateCronRequest(stateMachine.ConversationId, null, ChatState.CreateTask)),
            CompleteWorkflowAsync);
        
        _innerWorkflowManager.SetResponse(TaskCreateOrUpdateState.End, new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Task created successfully!"
        });
    }
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, long forUserId, string? taskName = null, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CreateTaskWorkflowStartedEvent(chatId), cancellationToken);
        
        if (_chatStates.ContainsKey(new ConversationUserKey(chatId, forUserId)))
        {
            // can't create a task while creating another task
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You cannot create a new task while you're still creating another one!"
            };
        }
        
        var machine = new TaskCreationStateMachine(chatId, forUserId, taskName);
        
        _chatStates.Add(new ConversationUserKey(chatId, forUserId), machine);

        return await _innerWorkflowManager.StartWorkflowAsync(machine, cancellationToken);
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, long forUserId, string userInput, CancellationToken cancellationToken = default)
    {
        if (!_chatStates.TryGetValue(new ConversationUserKey(chatId, forUserId), out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You're not creating a task right now!"
            };
        }
        
        return await _innerWorkflowManager.ContinueWorkflowAsync(machine, userInput, cancellationToken);
    }

    public Task AbandonWorkflowAsync(long chatId)
    {
        throw new NotImplementedException();
    }
    
    private async Task CompleteWorkflowAsync(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine)
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
        await _mediator.Send(new CreateTaskWorkflowCompletedEvent(stateMachine.ConversationId));
    }
}
