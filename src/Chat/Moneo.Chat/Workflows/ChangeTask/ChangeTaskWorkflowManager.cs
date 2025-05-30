using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.Chat.Workflows.CreateTask;
using Moneo.Common;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Chat.Workflows.ChangeTask;

public interface IChangeTaskWorkflowManager : ICreateOrUpdateTaskWorkflowManager;

[MoneoWorkflow]
public class ChangeTaskWorkflowManager : WorkflowManagerBase, IChangeTaskWorkflowManager
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChangeTaskWorkflowManager> _logger;
    private readonly ITaskManagerClient _taskResourceManager;
    private readonly CreateOrUpdateTaskWorkflowManager _innerWorkflowManager;
    private readonly IWorkflowWithTaskDraftStateMachineRepository _chatStates;
    private readonly Dictionary<long, ChangeTaskMenuOption> _menuOptions = new();
    
    public ChangeTaskWorkflowManager(
        IMediator mediator,
        ILogger<ChangeTaskWorkflowManager> logger,
        IWorkflowWithTaskDraftStateMachineRepository chatStates,
        ITaskManagerClient taskResourceManager) : base(mediator)
    {
        _mediator = mediator;
        _logger = logger;
        _taskResourceManager = taskResourceManager;
        _chatStates = chatStates;
        
        _innerWorkflowManager =
        new CreateOrUpdateTaskWorkflowManager(
            logger,
            stateMachine => mediator.Send(new CreateCronRequest(stateMachine.ConversationId, null, ChatState.ChangeTask)),
            CompleteWorkflowAsync);
        
        _innerWorkflowManager.SetResponseHandler(TaskCreateOrUpdateState.WaitingForUserDirection, HandleWaitingForUserSelection);
        InitializeMenuOptions();
        
        var sb = new StringBuilder();
        sb.AppendLine("What would you like to change about the task?");
        
        // still debating whether to use actual menu mechanism (which right now is Telegram-specific or allow free-form responses)
        /*
        foreach (var option in _menuOptions)
        {
            sb.AppendLine($"{option.Key}. {option.Value.Text}");
        }
        */
        
        _innerWorkflowManager.SetResponse(TaskCreateOrUpdateState.WaitingForUserDirection, new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = sb.ToString(),
            MenuOptions = _menuOptions.Values.Select(o => o.Text).ToHashSet()
        });
        
        _innerWorkflowManager.SetResponse(TaskCreateOrUpdateState.End, new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Task updated successfully!"
        });
    }
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string? taskName = null, CancellationToken cancellationToken = default)
    {
        if (_chatStates.ContainsKey(cmdContext.GenerateConversationUserKey()))
        {
            // can't create a task while creating another task
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You cannot change a task while you're still creating or changing another one!"
            };
        }
        
        if (string.IsNullOrEmpty(taskName))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You have to tell me which one you want to change."
            };
        }
        
        await _mediator.Send(new ChangeTaskWorkflowStartedEvent(cmdContext.ConversationId), cancellationToken);

        var searchResult = await _taskResourceManager.GetTasksByKeywordSearchAsync(
            cmdContext.ConversationId, 
            taskName, 
            new PageOptions(0, 100), cancellationToken);
        
        if (!searchResult.IsSuccess || searchResult.Data is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = searchResult.Message ?? 
                                  "An error occurred while searching for the task. Please try again later."
            };
        }

        var tasks = searchResult.Data?.Data ?? [];

        switch (tasks.Count)
        {
            case 0:
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = $"There was a problem working with {taskName} and I can't find it."
                };
            case > 1:
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Menu,
                    Type = ResultType.NeedMoreInfo,
                    UserMessageText = "There were multiple possible tasks that matched the description you gave",
                    MenuOptions = tasks.Select(t => $"/change {t.Name}").ToHashSet()
                };
            default:
                var machine = new TaskChangeStateMachine(cmdContext.ConversationId, new MoneoTaskDraft(cmdContext.User?.Id ?? 0, tasks.Single()));
                _chatStates.Add(cmdContext.GenerateConversationUserKey(), machine);
                return await _innerWorkflowManager.StartWorkflowAsync(machine, cancellationToken);
        }
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default)
    {
        if (!_chatStates.TryGetValue(cmdContext.GenerateConversationUserKey(), out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You're not changing a task right now!"
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
        var result = await _taskResourceManager.UpdateTaskAsync(
            stateMachine.ConversationId, 
            stateMachine.Draft.ToEditDto());

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Exception, "Failed to update task {TaskId}", stateMachine.ConversationId);
        }
        
        _chatStates.Remove(new ConversationUserKey(stateMachine.ConversationId, stateMachine.Draft.ForUserId));
        await _mediator.Send(new ChangeTaskWorkflowCompletedEvent(stateMachine.ConversationId));
    }

    private (bool Success, string? FailureMessage) HandleWaitingForUserSelection(
        IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine, 
        string userInput)
    {
        if (machine is not TaskChangeStateMachine typedMachine)
        {
            return (false, "An error occurred while processing your request. Please try again later.");
        }
        
        var option = GetTaskMenuOptionFromUserInput(userInput);

        if (option is null)
        {
            return (false, "I don't know what you mean. Please select one of the options.");
        }
        
        typedMachine.SetPendingState(option.NextState);
        return (true, null);
    }
    
    private ChangeTaskMenuOption? GetTaskMenuOptionFromUserInput(string userInput)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            return null;
        }
        
        if (int.TryParse(userInput.Trim(), out var optionNumber) && _menuOptions.TryGetValue(optionNumber, out var option))
        {
            return option;
        }

        var options = _menuOptions.Values.Where(o => o.Text.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        return options.Length == 1 ? options[0] : null;
    }
    
    private void InitializeMenuOptions()
    {
        _menuOptions[1] = new ChangeTaskMenuOption("1. Name", TaskCreateOrUpdateState.WaitingForName);
        _menuOptions[2] = new ChangeTaskMenuOption("2. Description", TaskCreateOrUpdateState.WaitingForDescription);
        _menuOptions[3] = new ChangeTaskMenuOption("3. Current Timezone", TaskCreateOrUpdateState.WaitingForTimezone);
        _menuOptions[4] = new ChangeTaskMenuOption("4. If/How The Task Repeats", TaskCreateOrUpdateState.WaitingForRepeater);
        _menuOptions[5] = new ChangeTaskMenuOption("5. Badgering You About The Task", TaskCreateOrUpdateState.WaitingForBadger);
        _menuOptions[6] = new ChangeTaskMenuOption("6. Due Dates", TaskCreateOrUpdateState.WaitingForDueDates);
        _menuOptions[7] = new ChangeTaskMenuOption("7. I'm Done", TaskCreateOrUpdateState.End);
    }
}

internal record ChangeTaskMenuOption(string Text, TaskCreateOrUpdateState NextState);
