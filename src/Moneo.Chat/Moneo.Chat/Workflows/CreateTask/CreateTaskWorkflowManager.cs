using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateTask;

public interface ICreateTaskWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
    Task CompleteWorkflowAsync(long chatId);
    Task AbandonWorkflowAsync(long chatId);
}

public class CreateTaskWorkflowManager : ICreateTaskWorkflowManager
{
    private readonly IMediator _mediator;
    private readonly Dictionary<long, TaskCreationStateMachine> _chatStates = new();
    private readonly Dictionary<TaskCreationState, string> _responseStore = new();
    private readonly ILogger<CreateTaskWorkflowManager> _logger;
    
    private void InitializeResponses()
    {
        _responseStore[TaskCreationState.WaitingForName] = CreateTaskResponse.AskForNameResponse;
        _responseStore[TaskCreationState.WaitingForDescription] = CreateTaskResponse.AskForDescriptionResponse;
        _responseStore[TaskCreationState.WaitingForSkippedMessage] = CreateTaskResponse.SkippedMessageResponse;
        _responseStore[TaskCreationState.WaitingForRepeater] = CreateTaskResponse.RepeaterResponse;
        _responseStore[TaskCreationState.WaitingForRepeaterCron] = CreateTaskResponse.RepeaterCronResponse;
        _responseStore[TaskCreationState.WaitingForRepeaterExpiry] = CreateTaskResponse.RepeaterExpiryResponse;
        _responseStore[TaskCreationState.WaitingForRepeaterCompletionThreshold] = CreateTaskResponse.RepeaterCompletionThreshold;
        _responseStore[TaskCreationState.WaitingForBadger] = CreateTaskResponse.BadgerResponse;
        _responseStore[TaskCreationState.WaitingForBadgerFrequency] = CreateTaskResponse.BadgerFrequencyResponse;
        _responseStore[TaskCreationState.WaitingForDueDates] = CreateTaskResponse.DueDatesResponse;
        _responseStore[TaskCreationState.End] = CreateTaskResponse.EndOfWorkflowResponse;
    }

    private string? GetResponseTextToState(TaskCreationState state)
    {
        _responseStore.TryGetValue(state, out var text);
        return text;
    }

    public CreateTaskWorkflowManager(IMediator mediator, ILogger<CreateTaskWorkflowManager> logger)
    {
        _mediator = mediator;
        _logger = logger;
        InitializeResponses();
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, string? taskName = null)
    {
        await _mediator.Send(new ConversationStateChangeEvent(chatId, ConversationState.CreateTask));
        
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

        var machine = new TaskCreationStateMachine(taskName);
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

        var draft = machine.GetCurrentDraft();
        
        _logger.LogDebug("Current State: {@State}", machine.CurrentState);

        switch (machine.CurrentState)
        {
            case TaskCreationState.WaitingForName:
                // let's hope the user gave us a name
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    // they didn't
                    return new MoneoCommandResult
                    {
                        ResponseType = ResponseType.Text,
                        Type = ResultType.Error,
                        UserMessageText = "No, a name. I need a name"
                    };
                }

                _logger.LogDebug("Setting Task Name To {@Name}", userInput);
                draft.Name = userInput;
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForDescription:
                _logger.LogDebug("Setting Task Description To {@Description}", userInput);
                draft.Description = userInput;
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForRepeater:
                if (userInput.Contains("yes", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Enabling Repeater");
                    machine.EnableRepeater();
                }
                else if (userInput.Contains("no", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Disabling Repeater");
                    machine.DisableRepeater();
                }
                else
                {
                    return new MoneoCommandResult
                    {
                        ResponseType = ResponseType.Text,
                        Type = ResultType.Error,
                        UserMessageText = "I'm sorry, that was a yes or no question"
                    };
                }
                break;
            case TaskCreationState.WaitingForRepeaterCron:
                draft.Repeater!.RepeatCron = userInput;
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForRepeaterExpiry:
                draft.Repeater!.Expiry = DateTime.Parse(userInput);
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForRepeaterCompletionThreshold:
                draft.Repeater!.EarlyCompletionThresholdHours = int.Parse(userInput);
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForBadger:
                if (userInput.Contains("yes", StringComparison.OrdinalIgnoreCase))
                {
                    machine.EnableBadger();
                }
                else if (userInput.Contains("no", StringComparison.OrdinalIgnoreCase))
                {
                    machine.DisableBadger();
                }
                else
                {
                    return new MoneoCommandResult
                    {
                        ResponseType = ResponseType.Text,
                        Type = ResultType.Error,
                        UserMessageText = "I'm sorry, that was a yes or no question"
                    };
                }
                break;
            case TaskCreationState.WaitingForBadgerFrequency:
                draft.Badger!.BadgerFrequencyMinutes = int.Parse(userInput);
                machine.UpdateDraft(draft);
                break;
            case TaskCreationState.WaitingForDueDates:
                draft.DueDates = userInput.Split(',').Select(DateTime.Parse).ToHashSet();
                machine.UpdateDraft(draft);
                break;
        }

        var responseText = GetResponseTextToState(machine.GoToNext());

        while (string.IsNullOrEmpty(responseText))
        {
            if (machine.CurrentState == TaskCreationState.WaitingForTimezone)
            {
                _logger.LogDebug("Setting Timezone");
                draft.TimeZone = "Pacific Standard Time";
                machine.UpdateDraft(draft);
            }
            else if (machine.CurrentState == TaskCreationState.WaitingForBadgerMessages)
            {
                draft.Badger!.BadgerMessages = new[]
                {
                    $"Hey! You need to do the {draft.Name} thing",
                    $"You still haven't finished your task: {draft.Name}",
                    $"Dude. {draft.Name}. It's past due."
                };
                machine.UpdateDraft(draft);
            }

            // do a thing and advance again
            responseText = GetResponseTextToState(machine.GoToNext());
        }

        if (machine.CurrentState == TaskCreationState.End)
        {
            await CompleteWorkflowAsync(chatId);
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = responseText
        };
    }

    public async Task CompleteWorkflowAsync(long chatId)
    {
        // this is where we should try to actually create the thing
        _chatStates.Remove(chatId);
        await _mediator.Send(new ConversationStateChangeEvent(chatId, ConversationState.Waiting));
    }

    public Task AbandonWorkflowAsync(long chatId)
    {
        throw new NotImplementedException();
    }
}