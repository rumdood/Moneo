using Moneo.Chat.Workflows.CreateTask;
using Moneo.TaskManagement.Models;

namespace Moneo.Chat.Workflows;

public interface IWorkflowStateMachine<out TState> where TState : Enum
{
    TState CurrentState { get; }
    TState GoToNext();
}

public interface IWorkflowWithTaskDraftStateMachine<out TState> : IWorkflowStateMachine<TState> where TState : Enum
{
    MoneoTaskDraft Draft { get; }
    long ConversationId { get; }
}
