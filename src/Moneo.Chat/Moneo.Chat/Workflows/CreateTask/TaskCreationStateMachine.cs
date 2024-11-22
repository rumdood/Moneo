namespace Moneo.Chat.Workflows.CreateTask;

public class TaskCreationStateMachine : IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>
{
    public TaskCreateOrUpdateState CurrentState { get; private set; }
    public MoneoTaskDraft Draft { get; } = new();
    public long ConversationId { get; private set; }

    public TaskCreationStateMachine(long conversationId, string? name = null)
    {
        ConversationId = conversationId;
        CurrentState = string.IsNullOrEmpty(name) ? TaskCreateOrUpdateState.Start : TaskCreateOrUpdateState.WaitingForName;

        if (!string.IsNullOrEmpty(name))
        {
            Draft.Task.Name = name;
        }
    }

    public TaskCreateOrUpdateState GoToNext()
    {
        switch (CurrentState)
        {
            case TaskCreateOrUpdateState.Start:
                CurrentState = TaskCreateOrUpdateState.WaitingForName;
                break;
            case TaskCreateOrUpdateState.WaitingForName:
                CurrentState = TaskCreateOrUpdateState.WaitingForDescription;
                break;
            case TaskCreateOrUpdateState.WaitingForDescription:
                CurrentState = TaskCreateOrUpdateState.WaitingForTimezone;
                break;
            case TaskCreateOrUpdateState.WaitingForTimezone:
                CurrentState = TaskCreateOrUpdateState.WaitingForCompletedMessage;
                break;
            case TaskCreateOrUpdateState.WaitingForCompletedMessage:
                CurrentState = TaskCreateOrUpdateState.WaitingForSkippedMessage;
                break;
            case TaskCreateOrUpdateState.WaitingForSkippedMessage:
                CurrentState = TaskCreateOrUpdateState.WaitingForRepeater;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeater:
                CurrentState = Draft.IsRepeaterEnabled
                    ? TaskCreateOrUpdateState.WaitingForRepeaterCron
                    : TaskCreateOrUpdateState.WaitingForBadger;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterCron:
                CurrentState = TaskCreateOrUpdateState.WaitingForRepeaterExpiry;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterExpiry:
                CurrentState = TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold:
                CurrentState = TaskCreateOrUpdateState.WaitingForBadger;
                break;
            case TaskCreateOrUpdateState.WaitingForBadger:
                CurrentState = Draft.IsBadgerEnabled
                    ? TaskCreateOrUpdateState.WaitingForBadgerFrequency
                    : Draft.IsRepeaterEnabled ? TaskCreateOrUpdateState.End : TaskCreateOrUpdateState.WaitingForDueDates;
                break;
            case TaskCreateOrUpdateState.WaitingForBadgerFrequency:
                CurrentState = TaskCreateOrUpdateState.WaitingForBadgerMessages;
                break;
            case TaskCreateOrUpdateState.WaitingForBadgerMessages:
                CurrentState = Draft.IsRepeaterEnabled ? TaskCreateOrUpdateState.End : TaskCreateOrUpdateState.WaitingForDueDates;
                break;
            case TaskCreateOrUpdateState.WaitingForDueDates:
                CurrentState = TaskCreateOrUpdateState.End;
                break;
            case TaskCreateOrUpdateState.End:
            default:
                break;
        }

        return CurrentState;
    }
}