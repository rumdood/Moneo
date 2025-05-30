using Moneo.Chat.Workflows.CreateCronSchedule;

namespace Moneo.Chat.Workflows;

public record ConversationUserKey(long ConversationId, long UserId);

public interface IWorkflowWithTaskDraftStateMachineRepository
{
    bool ContainsKey(ConversationUserKey key);
    void Add(ConversationUserKey key, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    bool TryGetValue(ConversationUserKey key, out IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    void Remove(ConversationUserKey key);
}

public interface IWorkflowStateMachineRepository<TState> where TState : Enum
{
    bool ContainsKey(ConversationUserKey key);
    void Add(ConversationUserKey key, IWorkflowStateMachine<TState> stateMachine);
    bool TryGetValue(ConversationUserKey key, out IWorkflowStateMachine<TState> stateMachine);
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

internal class CronStateMachineRepository : IWorkflowStateMachineRepository<CronWorkflowState>
{
    private readonly Dictionary<ConversationUserKey, IWorkflowStateMachine<CronWorkflowState>> _chatStates = new();
    
    public bool ContainsKey(ConversationUserKey key) => _chatStates.ContainsKey(key);

    public void Add(ConversationUserKey key, IWorkflowStateMachine<CronWorkflowState> stateMachine)
        => _chatStates.Add(key, stateMachine);

    public bool TryGetValue(ConversationUserKey key, out IWorkflowStateMachine<CronWorkflowState> stateMachine)
        => _chatStates.TryGetValue(key, out stateMachine);

    public void Remove(ConversationUserKey key) => _chatStates.Remove(key);
}
