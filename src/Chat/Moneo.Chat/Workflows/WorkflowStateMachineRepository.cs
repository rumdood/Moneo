using Moneo.Chat.Workflows.CreateCronSchedule;

namespace Moneo.Chat.Workflows;

public interface IWorkflowWithTaskDraftStateMachineRepository
{
    bool ContainsKey(long conversationId);
    void Add(long conversationId, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    bool TryGetValue(long conversationId, out IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine);
    void Remove(long conversationId);
}

public interface IWorkflowStateMachineRepository<TState> where TState : Enum
{
    bool ContainsKey(long conversationId);
    void Add(long conversationId, IWorkflowStateMachine<TState> stateMachine);
    bool TryGetValue(long conversationId, out IWorkflowStateMachine<TState> stateMachine);
    void Remove(long conversationId);
}

internal class TaskCreateOrChangeStateMachineRepository : IWorkflowWithTaskDraftStateMachineRepository
{
    private readonly Dictionary<long, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState>> _chatStates = new();
    
    public bool ContainsKey(long conversationId) => _chatStates.ContainsKey(conversationId);

    public void Add(long conversationId, IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine)
        => _chatStates.Add(conversationId, stateMachine);

    public bool TryGetValue(long conversationId, out IWorkflowWithTaskDraftStateMachine<TaskCreateOrUpdateState> stateMachine)
        => _chatStates.TryGetValue(conversationId, out stateMachine);

    public void Remove(long conversationId) => _chatStates.Remove(conversationId);
}

internal class CronStateMachineRepository : IWorkflowStateMachineRepository<CronWorkflowState>
{
    private readonly Dictionary<long, IWorkflowStateMachine<CronWorkflowState>> _chatStates = new();
    
    public bool ContainsKey(long conversationId) => _chatStates.ContainsKey(conversationId);

    public void Add(long conversationId, IWorkflowStateMachine<CronWorkflowState> stateMachine)
        => _chatStates.Add(conversationId, stateMachine);

    public bool TryGetValue(long conversationId, out IWorkflowStateMachine<CronWorkflowState> stateMachine)
        => _chatStates.TryGetValue(conversationId, out stateMachine);

    public void Remove(long conversationId) => _chatStates.Remove(conversationId);
}
