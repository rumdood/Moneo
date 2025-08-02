using Moneo.Chat.Workflows;
using Moneo.Hosts.Chat.Api.Tasks;

namespace Moneo.TaskManagement.Workflows;

public interface IWorkflowWithTaskDraftStateMachineRepository
{
    bool ContainsKey(ConversationUserKey key);
    void Add(ConversationUserKey key, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    bool TryGetValue(ConversationUserKey key, out IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    void Remove(ConversationUserKey key);
}

internal class TaskCreateOrChangeStateMachineRepository : IWorkflowWithTaskDraftStateMachineRepository
{
    private readonly Dictionary<ConversationUserKey, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>> _chatStates = new();
    
    public bool ContainsKey(ConversationUserKey key) => _chatStates.ContainsKey(key);

    public void Add(ConversationUserKey key, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine)
        => _chatStates.Add(key, stateMachine);

    public bool TryGetValue(ConversationUserKey key, out IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine)
        => _chatStates.TryGetValue(key, out stateMachine);

    public void Remove(ConversationUserKey key) => _chatStates.Remove(key);
}
