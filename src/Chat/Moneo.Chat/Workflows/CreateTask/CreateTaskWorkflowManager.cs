using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Chat.Workflows.CreateTask;

public interface ICreateTaskWorkflowManager : ICreateOrUpdateTaskWorkflowManager, IWorkflowManager;

// TODO: Add some default Completed messages
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
            stateMachine => mediator.Send(new CreateCronRequest(stateMachine.ConversationId, ChatState.CreateTask)),
            CompleteWorkflowAsync);
        
        _innerWorkflowManager.SetResponse(TaskCreateOrUpdateState.End, new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Task created successfully!"
        });
    }
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null)
    {
        await _mediator.Send(new CreateTaskWorkflowStartedEvent(chatId));
        
        if (_chatStates.ContainsKey(chatId))
        {
            // can't create a task while creating another task
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You cannot create a new task while you're still creating another one!"
            };
        }
        
        var machine = new TaskCreationStateMachine(chatId, taskName);
        
        _chatStates.Add(chatId, machine);

        return await _innerWorkflowManager.StartWorkflowAsync(machine);
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        if (!_chatStates.TryGetValue(chatId, out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You're not creating a task right now!"
            };
        }
        
        return await _innerWorkflowManager.ContinueWorkflowAsync(machine, userInput);
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
        
        _chatStates.Remove(stateMachine.ConversationId);
        await _mediator.Send(new CreateTaskWorkflowCompletedEvent(stateMachine.ConversationId));
    }
}
