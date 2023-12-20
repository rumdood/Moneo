using Moneo.TaskManagement.Models;

namespace Moneo.Chat.Workflows.CreateTask;

internal enum TaskCreationState
{
    Start,
    WaitingForName,
    WaitingForDescription,
    WaitingForTimezone,
    WaitingForCompletedMessage,
    WaitingForSkippedMessage,
    WaitingForRepeater,
    WaitingForRepeaterCron,
    WaitingForRepeaterExpiry,
    WaitingForRepeaterCompletionThreshold,
    WaitingForBadger,
    WaitingForBadgerFrequency,
    WaitingForBadgerMessages,
    WaitingForDueDates,
    End
}

internal class TaskCreationStateMachine : IWorkflowStateMachine<TaskCreationState>, IWorkflowDraftEditor
{
    private MoneoTaskDto _draft;
    private bool _repeaterEnabled;
    private bool _badgerEnabled;

    public TaskCreationState CurrentState { get; private set; }

    public TaskCreationStateMachine(string? name = null)
    {
        CurrentState = string.IsNullOrEmpty(name) ? TaskCreationState.Start : TaskCreationState.WaitingForName;
        _draft = new MoneoTaskDto();
    }
    
    public MoneoTaskDto GetCurrentDraft() => _draft;

    public void UpdateDraft(MoneoTaskDto draft)
    {
        _draft = draft;
    }

    public void EnableRepeater()
    {
        _repeaterEnabled = true;
        _draft.Repeater = new TaskRepeater();
    }

    public void DisableRepeater()
    {
        _repeaterEnabled = false;
        _draft.Repeater = null;
    }

    public void EnableBadger()
    {
        _badgerEnabled = true;
        _draft.Badger = new TaskBadger();
    }

    public void DisableBadger()
    {
        _badgerEnabled = false;
        _draft.Badger = null;
    }

    public TaskCreationState GoToNext()
    {
        switch (CurrentState)
        {
            case TaskCreationState.Start:
                CurrentState = TaskCreationState.WaitingForName;
                break;
            case TaskCreationState.WaitingForName:
                CurrentState = TaskCreationState.WaitingForDescription;
                break;
            case TaskCreationState.WaitingForDescription:
                CurrentState = TaskCreationState.WaitingForTimezone;
                break;
            case TaskCreationState.WaitingForTimezone:
                CurrentState = TaskCreationState.WaitingForCompletedMessage;
                break;
            case TaskCreationState.WaitingForCompletedMessage:
                CurrentState = TaskCreationState.WaitingForSkippedMessage;
                break;
            case TaskCreationState.WaitingForSkippedMessage:
                CurrentState = TaskCreationState.WaitingForRepeater;
                break;
            case TaskCreationState.WaitingForRepeater:
                CurrentState = _repeaterEnabled ? TaskCreationState.WaitingForRepeaterCron : TaskCreationState.WaitingForBadger;
                break;
            case TaskCreationState.WaitingForRepeaterCron:
                CurrentState = TaskCreationState.WaitingForRepeaterExpiry;
                break;
            case TaskCreationState.WaitingForRepeaterExpiry:
                CurrentState = TaskCreationState.WaitingForRepeaterCompletionThreshold;
                break;
            case TaskCreationState.WaitingForRepeaterCompletionThreshold:
                CurrentState = TaskCreationState.WaitingForBadger;
                break;
            case TaskCreationState.WaitingForBadger:
                CurrentState = _badgerEnabled
                    ? TaskCreationState.WaitingForBadgerFrequency
                    : _repeaterEnabled ? TaskCreationState.End : TaskCreationState.WaitingForDueDates;
                break;
            case TaskCreationState.WaitingForBadgerFrequency:
                CurrentState = TaskCreationState.WaitingForBadgerMessages;
                break;
            case TaskCreationState.WaitingForBadgerMessages:
                CurrentState = _repeaterEnabled ? TaskCreationState.End : TaskCreationState.WaitingForDueDates;
                break;
            case TaskCreationState.WaitingForDueDates:
                CurrentState = TaskCreationState.End;
                break;
            case TaskCreationState.End:
            default:
                break;
        }

        return CurrentState;
    }
}