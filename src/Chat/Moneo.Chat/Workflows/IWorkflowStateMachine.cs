namespace Moneo.Chat.Workflows;

public interface IWorkflowStateMachine<out TState> where TState : Enum
{
    TState CurrentState { get; }
    TState GoToNext();
}
