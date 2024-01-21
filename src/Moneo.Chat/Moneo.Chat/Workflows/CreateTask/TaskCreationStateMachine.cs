namespace Moneo.Chat.Workflows.CreateTask;

internal class TaskCreationStateMachine : IWorkflowStateMachine<TaskCreationState>
{
    public TaskCreationState CurrentState { get; private set; }
    public MoneoTaskDraft Draft { get; } = new();
    public long ConversationId { get; private set; }

    public TaskCreationStateMachine(long conversationId, string? name = null)
    {
        ConversationId = conversationId;
        CurrentState = string.IsNullOrEmpty(name) ? TaskCreationState.Start : TaskCreationState.WaitingForName;

        if (!string.IsNullOrEmpty(name))
        {
            Draft.Task.Name = name;
        }
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
                CurrentState = Draft.IsRepeaterEnabled
                    ? TaskCreationState.WaitingForRepeaterCron
                    : TaskCreationState.WaitingForBadger;
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
                CurrentState = Draft.IsBadgerEnabled
                    ? TaskCreationState.WaitingForBadgerFrequency
                    : Draft.IsRepeaterEnabled ? TaskCreationState.End : TaskCreationState.WaitingForDueDates;
                break;
            case TaskCreationState.WaitingForBadgerFrequency:
                CurrentState = TaskCreationState.WaitingForBadgerMessages;
                break;
            case TaskCreationState.WaitingForBadgerMessages:
                CurrentState = Draft.IsRepeaterEnabled ? TaskCreationState.End : TaskCreationState.WaitingForDueDates;
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