using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat.Workflows;

public interface ICreateOrUpdateTaskWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string? taskName = null, CancellationToken cancellationToken = default);
    Task AbandonWorkflowAsync(long chatId);
}

public interface ITaskWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(
        IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine, CancellationToken cancellationToken);

    Task<MoneoCommandResult> ContinueWorkflowAsync(
        CommandContext context,
        IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine, 
        string userInput, 
        CancellationToken cancellationToken);
}

[MoneoWorkflow]
public class CreateOrUpdateTaskWorkflowManager : ITaskWorkflowManager
{
    private readonly ILogger _logger;
    // private readonly Dictionary<TaskCreateOrUpdateState, string> _responseStore = new();
    private readonly Dictionary<TaskCreateOrUpdateState, MoneoCommandResult> _responseStore = new();

    private readonly
        Dictionary<TaskCreateOrUpdateState, Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, string, (
            bool Success, string? FailureMessage)>>
        _responseHandlers = new();
    private readonly Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, CommandContext, Task> _onComplete;
    private readonly Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, Task<MoneoCommandResult>> _onReadyForRepeaterCron;
    private readonly string _defaultTimezone;

    public CreateOrUpdateTaskWorkflowManager(
        ILogger logger,
        Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, Task<MoneoCommandResult>> onReadyForRepeaterCron,
        Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, CommandContext, Task> onComplete,
        string defaultTimezone = "America/Los_Angeles")
    {
        _logger = logger;
        _onComplete = onComplete;
        _onReadyForRepeaterCron = onReadyForRepeaterCron;
        _defaultTimezone = defaultTimezone;
        InitializeResponses();
        InitializeResponseHandlers();
    }

    private (bool Success, string? FailureMessage) HandleTaskNameInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine, string userInput)
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

    private (bool Success, string? FailureMessage) HandleTaskDescriptionInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        _logger.LogDebug("Setting Task Description To {@Description}", userInput);
        machine.Draft.Task.Description = userInput.Equals("none", StringComparison.OrdinalIgnoreCase)
            ? ""
            : userInput;
        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
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

    private (bool Success, string? FailureMessage) HandleTaskRepeaterCronInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        machine.Draft.Repeater!.CronExpression = userInput;
        return (true, null);
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterExpiryInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        var noExpiry = new HashSet<string> { "none", "never", "no", "n/a", "it doesn't"};
        try
        {
            machine.Draft.Repeater!.Expiry = DateTime.Parse(userInput);
            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Repeater Expiry");
            return (false, "I didn't understand that as a date");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskRepeaterThresholdInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        try
        {
            var threshold =
                string.IsNullOrWhiteSpace(userInput) || userInput.Equals("default", StringComparison.OrdinalIgnoreCase)
                    ? 3
                    : int.Parse(userInput);
        
            machine.Draft.Repeater!.EarlyCompletionThresholdHours = threshold;

            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Repeater Completion Threshold");
            return (false, "The threshold has to be a number");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskBadgerInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
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

    private (bool Success, string? FailureMessage) HandleTaskBadgerFrequencyInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        try
        {
            machine.Draft.Badger!.BadgerFrequencyInMinutes = int.Parse(userInput);
            return (true, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing Badger Frequency");
            return (false, "The badger frequency has to be a number");
        }
    }

    private (bool Success, string? FailureMessage) HandleTaskDueDatesInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        if (!DateTime.TryParse(userInput, out var dueDate))
        {
            return (false, "That's not a valid date");
        }
        
        machine.Draft.Task.DueOn = dueDate;
        return (true, null);
    }
    
    private (bool Success, string? FailureMessage) HandleTimeZoneInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        _logger.LogDebug("Setting Timezone");
        machine.Draft.Task.Timezone = userInput;
        return (true, null);
    }
    
    private (bool Success, string? FailureMessage) HandleBadgerMessagesInput(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> machine,
        string userInput)
    {
        machine.Draft.Badger!.BadgerMessages =
        [
            $"Hey! You need to do the {machine.Draft.Task.Name} thing",
            $"You still haven't finished your task: {machine.Draft.Task.Name}",
            $"Dude. {machine.Draft.Task.Name}. It's past due."
        ];
        return (true, null);
    }
    
    private static MoneoCommandResult GetTextCommandResult(string text, ResultType rType = ResultType.NeedMoreInfo)
    {
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = rType,
            UserMessageText = text
        };
    }    
    
    private void InitializeResponses()
    {
        _responseStore[TaskCreateOrUpdateState.WaitingForName] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.AskForNameResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForDescription] = GetTextCommandResult(CreateOrUpdateTaskResponse.AskForDescriptionResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForSkippedMessage] = GetTextCommandResult(CreateOrUpdateTaskResponse.SkippedMessageResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForRepeater] = GetTextCommandResult(CreateOrUpdateTaskResponse.RepeaterResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForRepeaterExpiry] = GetTextCommandResult(CreateOrUpdateTaskResponse.RepeaterExpiryResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.RepeaterCompletionThreshold);
        _responseStore[TaskCreateOrUpdateState.WaitingForBadger] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.BadgerResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForBadgerFrequency] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.BadgerFrequencyResponse);
        _responseStore[TaskCreateOrUpdateState.WaitingForDueDates] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.DueDatesResponse);
        _responseStore[TaskCreateOrUpdateState.End] =
            GetTextCommandResult(CreateOrUpdateTaskResponse.EndOfWorkflowResponse, ResultType.WorkflowCompleted);
    }
    
    private void InitializeResponseHandlers()
    {
        _responseHandlers[TaskCreateOrUpdateState.WaitingForName] = HandleTaskNameInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForDescription] = HandleTaskDescriptionInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForRepeater] = HandleTaskRepeaterInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForRepeaterCron] = HandleTaskRepeaterCronInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForRepeaterExpiry] = HandleTaskRepeaterExpiryInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold] = HandleTaskRepeaterThresholdInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForBadger] = HandleTaskBadgerInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForBadgerFrequency] = HandleTaskBadgerFrequencyInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForDueDates] = HandleTaskDueDatesInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForTimezone] = HandleTimeZoneInput;
        _responseHandlers[TaskCreateOrUpdateState.WaitingForBadgerMessages] = HandleBadgerMessagesInput;
    }

    private MoneoCommandResult? GetResponseToState(TaskCreateOrUpdateState state)
    {
        _responseStore.TryGetValue(state, out var result);
        return result;
    }
    
    public void SetResponseText(TaskCreateOrUpdateState state, string responseText)
    {
        _responseStore[state] = GetTextCommandResult(responseText);
    }
    
    public void SetResponse(TaskCreateOrUpdateState state, MoneoCommandResult response)
    {
        _responseStore[state] = response;
    }

    public void SetResponseHandler(TaskCreateOrUpdateState state,
        Func<IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>, 
            string, (bool Success, string? FailureMessage)> handler)
    {
        _responseHandlers[state] = handler;
    }

    public Task<MoneoCommandResult> StartWorkflowAsync(IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine, CancellationToken cancellationToken = default)
    {
        var nextState = stateMachine.GoToNext();
        
        var response = GetResponseToState(nextState);

        if (response is null)
        {
            // what the hell happened? Throw this away
            return Task.FromResult(new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Something has gone horribly, horribly wrong and I have to throw all that away"
            });
        }

        return Task.FromResult(response);
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(
        CommandContext context,
        IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine, 
        string userInput, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Current State: {@State}", stateMachine.CurrentState);

        if (_responseHandlers.TryGetValue(stateMachine.CurrentState, out var handler))
        {
            var result = handler.Invoke(stateMachine, userInput);

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

        var response = GetResponseToState(stateMachine.GoToNext());

        while (response is null)
        {
            switch (stateMachine.CurrentState)
            {
                case TaskCreateOrUpdateState.WaitingForTimezone:
                    // TODO: Find a way to get the timezone from the user
                    HandleTimeZoneInput(stateMachine, _defaultTimezone);
                    break;
                case TaskCreateOrUpdateState.WaitingForBadgerMessages:
                    HandleBadgerMessagesInput(stateMachine, "");
                    break;
                case TaskCreateOrUpdateState.WaitingForRepeaterCron:
                    return await _onReadyForRepeaterCron.Invoke(stateMachine);
            }

            // do a thing and advance again
            response = GetResponseToState(stateMachine.GoToNext());
        }

        switch (stateMachine.CurrentState)
        {
            case TaskCreateOrUpdateState.End:
                await _onComplete(stateMachine, context);
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterCron:
                return await _onReadyForRepeaterCron.Invoke(stateMachine);
        }

        return response;
    }
}