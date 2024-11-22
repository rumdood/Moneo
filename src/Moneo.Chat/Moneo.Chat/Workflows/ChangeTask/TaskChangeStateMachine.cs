using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat.Workflows.ChangeTask;

public class TaskChangeStateMachine : IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>
{
    private TaskCreateOrUpdateState? _pendingState;
    
    public TaskCreateOrUpdateState CurrentState { get; private set; }
    public MoneoTaskDraft Draft { get; private set; }
    public long ConversationId { get; private set; }

    public TaskChangeStateMachine(
        long conversationId,
        MoneoTaskDraft draft,
        TaskCreateOrUpdateState initialState = TaskCreateOrUpdateState.Start)
    {
        ConversationId = conversationId;
        Draft = draft;
        CurrentState = initialState;
    }
    
    public void SetPendingState(TaskCreateOrUpdateState state)
    {
        _pendingState = state;
    }
    
    public TaskCreateOrUpdateState GoToNext()
    {
        switch (CurrentState)
        {
            case TaskCreateOrUpdateState.WaitingForRepeater:
                CurrentState = Draft.IsRepeaterEnabled
                    ? TaskCreateOrUpdateState.WaitingForRepeaterCron
                    : TaskCreateOrUpdateState.WaitingForBadger;
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
            case TaskCreateOrUpdateState.WaitingForRepeaterCron:
                CurrentState = TaskCreateOrUpdateState.WaitingForRepeaterExpiry;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterExpiry:
                CurrentState = TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold;
                break;
            case TaskCreateOrUpdateState.WaitingForRepeaterCompletionThreshold:
                CurrentState = TaskCreateOrUpdateState.WaitingForBadger;
                break;
            case TaskCreateOrUpdateState.Start:
            case TaskCreateOrUpdateState.WaitingForName:
            case TaskCreateOrUpdateState.WaitingForDescription:
            case TaskCreateOrUpdateState.WaitingForTimezone:
            case TaskCreateOrUpdateState.WaitingForCompletedMessage:
            case TaskCreateOrUpdateState.WaitingForSkippedMessage:
            case TaskCreateOrUpdateState.WaitingForDueDates:
                CurrentState = TaskCreateOrUpdateState.WaitingForUserDirection;
                break;
            case TaskCreateOrUpdateState.WaitingForUserDirection:
                CurrentState = _pendingState ?? TaskCreateOrUpdateState.WaitingForUserDirection;
                break;
            case TaskCreateOrUpdateState.End:
            default:
                break;
        }

        _pendingState = null;

        return CurrentState;
    }
}