using Moneo.Chat.Workflows;
using Moneo.TaskManagement.Workflows.CreateTask;

namespace Moneo.Hosts.Chat.Api.Tasks;

public interface IWorkflowWithTaskDraftStateMachine<out TState> : IWorkflowStateMachine<TState> where TState : Enum
{
    MoneoTaskDraft Draft { get; }
    long ConversationId { get; }
}