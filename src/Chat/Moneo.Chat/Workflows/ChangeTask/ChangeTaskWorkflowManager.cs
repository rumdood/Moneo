using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.Chat.Workflows.CreateTask;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client.Models;

namespace Moneo.Chat.Workflows.ChangeTask;

public interface IChangeTaskWorkflowManager : ICreateOrUpdateTaskWorkflowManager, IWorkflowManager;

public class ChangeTaskWorkflowManager : IChangeTaskWorkflowManager
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChangeTaskWorkflowManager> _logger;
    private readonly ITaskResourceManager _taskResourceManager;
    private readonly CreateOrUpdateTaskWorkflowManager _innerWorkflowManager;
    private readonly Dictionary<long, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>> _chatStates = new();
    private readonly Dictionary<long, ChangeTaskMenuOption> _menuOptions = new();
    
    public ChangeTaskWorkflowManager(
        IMediator mediator,
        ILogger<ChangeTaskWorkflowManager> logger,
        ITaskResourceManager taskResourceManager)
    {
        _mediator = mediator;
        _logger = logger;
        _taskResourceManager = taskResourceManager;
        
        _innerWorkflowManager =
        new CreateOrUpdateTaskWorkflowManager(
            logger,
            stateMachine => mediator.Send(new CreateCronRequest(stateMachine.ConversationId, ChatState.ChangeTask)),
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
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null)
    {
        if (_chatStates.ContainsKey(chatId))
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
        
        await _mediator.Send(new ChangeTaskWorkflowStartedEvent(chatId));

        var searchResult = await _taskResourceManager.GetTasksForUserAsync(
            chatId, 
            new MoneoTaskFilter { SearchString = taskName });
            
        if (!searchResult.IsSuccessful)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = searchResult.ErrorMessage ?? 
                                  "An error occurred while searching for the task. Please try again later."
            };
        }

        var tasks = searchResult.Result.Where(t => !string.IsNullOrEmpty(t.Id)).ToArray();

        switch (tasks.Length)
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
                var machine = new TaskChangeStateMachine(chatId, new MoneoTaskDraft(tasks.Single()));
                _chatStates.Add(chatId, machine);
                return await _innerWorkflowManager.StartWorkflowAsync(machine);
        }
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        if (!_chatStates.TryGetValue(chatId, out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You're not changing a task right now!"
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
        await _taskResourceManager.UpdateTaskAsync(
            stateMachine.ConversationId, 
            stateMachine.Draft.Task.Id,
            stateMachine.Draft.Task);
        _chatStates.Remove(stateMachine.ConversationId);
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
