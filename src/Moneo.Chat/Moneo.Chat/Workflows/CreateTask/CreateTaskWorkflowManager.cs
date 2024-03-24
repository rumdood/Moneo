using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.TaskManagement;

namespace Moneo.Chat.Workflows.CreateTask;

public interface ICreateTaskWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
    Task AbandonWorkflowAsync(long chatId);
}

public class CreateTaskWorkflowManager : WorkflowManagerBase, ICreateTaskWorkflowManager
{
    private readonly ILogger<CreateTaskWorkflowManager> _logger;
    private readonly Dictionary<long, TaskCreationStateMachine> _chatStates = new();
    private readonly Dictionary<TaskCreationState, string> _responseStore = new();
    private readonly
        Dictionary<TaskCreationState, Func<TaskCreationStateMachine, string, (bool Success, string? FailureMessage)>>
        _responseHandlers = new();

    private readonly ITaskResourceManager _resourceManager;

    private (bool Success, string? FailureMessage) HandleTaskNameInput(TaskCreationStateMachine machine, string userInput)
    {
        // let's hope the user gave us a name
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return (false, "No, a name. I need a name");
        }

        _logger.LogDebug("Setting Task Name To {@Name}", userInput);
        machine.Draft.Task.Name = userInput;

        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskDescriptionInput(TaskCreationStateMachine machine,
        string userInput)
    {
        _logger.LogDebug("Setting Task Description To {@Description}", userInput);
        machine.Draft.Task.Description = userInput.Equals("none", StringComparison.OrdinalIgnoreCase)
            ? ""
            : userInput;
        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterInput(TaskCreationStateMachine machine,
        string userInput)
    {
        if (userInput.Contains("yes", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Enabling Repeater");
            machine.Draft.EnableRepeater();
        }
        else if (userInput.Contains("no", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Disabling Repeater");
            machine.Draft.DisableRepeater();
        }
        else
        {
            return (false, "I'm sorry, that was a yes or no question");
        }

        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterCronInput(TaskCreationStateMachine machine,
        string userInput)
    {
        machine.Draft.Task.Repeater!.RepeatCron = userInput;
        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterExpiryInput(TaskCreationStateMachine machine,
        string userInput)
    {
        var noExpiry = new HashSet<string> { "none", "never", "no", "n/a", "it doesn't"};
        try
        {
            machine.Draft.Task.Repeater!.Expiry = DateTime.Parse(userInput);
            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Repeater Expiry");
            return (false, "I didn't understand that as a date");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterThresholdInput(TaskCreationStateMachine machine,
        string userInput)
    {
        try
        {
            var threshold =
                string.IsNullOrWhiteSpace(userInput) || userInput.Equals("default", StringComparison.OrdinalIgnoreCase)
                    ? 4
                    : int.Parse(userInput);
        
            machine.Draft.Task.Repeater!.EarlyCompletionThresholdHours = threshold;

            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Repeater Completion Threshold");
            return (false, "The threshold has to be a number");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskBadgerInput(TaskCreationStateMachine machine,
        string userInput)
    {
        if (userInput.Contains("yes", StringComparison.OrdinalIgnoreCase))
        {
            machine.Draft.EnableBadger();
        }
        else if (userInput.Contains("no", StringComparison.OrdinalIgnoreCase))
        {
            machine.Draft.DisableBadger();
        }
        else
        {
            return (false, "I'm sorry, that was a yes or no question");
        }

        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskBadgerFrequencyInput(TaskCreationStateMachine machine,
        string userInput)
    {
        try
        {
            machine.Draft.Task.Badger!.BadgerFrequencyMinutes = int.Parse(userInput);
            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Badger Frequency");
            return (false, "The badger frequency has to be a number");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskDueDatesInput(TaskCreationStateMachine machine,
        string userInput)
    {
        if (!DateTime.TryParse(userInput, out var dueDate))
        {
            return (false, "That's not a valid date");
        }
        
        machine.Draft.Task.DueDates.Add(dueDate);
        return (true, null);
    }
    
    private void InitializeResponses()
    {
        _responseStore[TaskCreationState.WaitingForName] = CreateTaskResponse.AskForNameResponse;
        _responseStore[TaskCreationState.WaitingForDescription] = CreateTaskResponse.AskForDescriptionResponse;
        _responseStore[TaskCreationState.WaitingForSkippedMessage] = CreateTaskResponse.SkippedMessageResponse;
        _responseStore[TaskCreationState.WaitingForRepeater] = CreateTaskResponse.RepeaterResponse;
        _responseStore[TaskCreationState.WaitingForRepeaterExpiry] = CreateTaskResponse.RepeaterExpiryResponse;
        _responseStore[TaskCreationState.WaitingForRepeaterCompletionThreshold] = CreateTaskResponse.RepeaterCompletionThreshold;
        _responseStore[TaskCreationState.WaitingForBadger] = CreateTaskResponse.BadgerResponse;
        _responseStore[TaskCreationState.WaitingForBadgerFrequency] = CreateTaskResponse.BadgerFrequencyResponse;
        _responseStore[TaskCreationState.WaitingForDueDates] = CreateTaskResponse.DueDatesResponse;
        _responseStore[TaskCreationState.End] = CreateTaskResponse.EndOfWorkflowResponse;
    }
    
    private void InitializeResponseHandlers()
    {
        _responseHandlers[TaskCreationState.WaitingForName] = HandleTaskNameInput;
        _responseHandlers[TaskCreationState.WaitingForDescription] = HandleTaskDescriptionInput;
        _responseHandlers[TaskCreationState.WaitingForRepeater] = HandleTaskRepeaterInput;
        _responseHandlers[TaskCreationState.WaitingForRepeaterCron] = HandleTaskRepeaterCronInput;
        _responseHandlers[TaskCreationState.WaitingForRepeaterExpiry] = HandleTaskRepeaterExpiryInput;
        _responseHandlers[TaskCreationState.WaitingForRepeaterCompletionThreshold] = HandleTaskRepeaterThresholdInput;
        _responseHandlers[TaskCreationState.WaitingForBadger] = HandleTaskBadgerInput;
        _responseHandlers[TaskCreationState.WaitingForBadgerFrequency] = HandleTaskBadgerFrequencyInput;
        _responseHandlers[TaskCreationState.WaitingForDueDates] = HandleTaskDueDatesInput;
    }

    private string? GetResponseTextToState(TaskCreationState state)
    {
        _responseStore.TryGetValue(state, out var text);
        return text;
    }

    public CreateTaskWorkflowManager(IMediator mediator, ILogger<CreateTaskWorkflowManager> logger,
        ITaskResourceManager taskResourceManager) : base(mediator)
    {
        _logger = logger;
        _resourceManager = taskResourceManager;
        InitializeResponses();
        InitializeResponseHandlers();
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null)
    {
        await Mediator.Send(new CreateTaskWorkflowStartedEvent(chatId));
        
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
        var nextState = machine.GoToNext();
        
        var responseText = GetResponseTextToState(nextState);

        if (string.IsNullOrEmpty(responseText))
        {
            // what the hell happened? Throw this away
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Something has gone horribly, horribly wrong and I have to throw all that away"
            };
        }
        
        // save the machine
        _chatStates[chatId] = machine;

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = responseText,
        };
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        if (!_chatStates.TryGetValue(chatId, out var machine))
        {
            _logger.LogCritical("Could not find pending workflow for task creation for chat {@ChatId}", chatId);
            return await StartWorkflowAsync(chatId, userInput);
        }

        var draft = machine.Draft;
        
        _logger.LogDebug("Current State: {@State}", machine.CurrentState);

        if (_responseHandlers.TryGetValue(machine.CurrentState, out var handler))
        {
            var result = handler.Invoke(machine, userInput);

            if (!result.Success)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = result.FailureMessage
                };
            }
        }

        var responseText = GetResponseTextToState(machine.GoToNext());

        while (string.IsNullOrEmpty(responseText))
        {
            if (machine.CurrentState == TaskCreationState.WaitingForTimezone)
            {
                _logger.LogDebug("Setting Timezone");
                draft.Task.TimeZone = "Pacific Standard Time";
            }
            else if (machine.CurrentState == TaskCreationState.WaitingForBadgerMessages)
            {
                draft.Task.Badger!.BadgerMessages = new[]
                {
                    $"Hey! You need to do the {draft.Task.Name} thing",
                    $"You still haven't finished your task: {draft.Task.Name}",
                    $"Dude. {draft.Task.Name}. It's past due."
                };
            }

            // do a thing and advance again
            responseText = GetResponseTextToState(machine.GoToNext());
        }

        if (machine.CurrentState == TaskCreationState.End)
        {
            await CompleteWorkflowAsync(chatId, draft);
        }
        else if (machine.CurrentState == TaskCreationState.WaitingForRepeaterCron)
        {
            return await Mediator.Send(new CreateCronRequest(machine.ConversationId));
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = responseText
        };
    }

    private async Task CompleteWorkflowAsync(long chatId, MoneoTaskDraft draft)
    {
        await _resourceManager.CreateTaskAsync(chatId, draft.Task);
        _chatStates.Remove(chatId);
        await Mediator.Send(new CreateTaskWorkflowCompletedEvent(chatId));
    }

    public Task AbandonWorkflowAsync(long chatId)
    {
        throw new NotImplementedException();
    }
}