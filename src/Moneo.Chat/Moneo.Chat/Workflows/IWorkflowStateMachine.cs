using Moneo.TaskManagement.Models;

namespace Moneo.Chat.Workflows;

public interface IWorkflowStateMachine<out TState> where TState : Enum
{
    TState CurrentState { get; }
    TState GoToNext();
}

public interface IWorkflowDraftEditor
{
    MoneoTaskDto GetCurrentDraft();
    void UpdateDraft(MoneoTaskDto draft);
    void EnableRepeater();
    void DisableRepeater();
    void EnableBadger();
    void DisableBadger();
}