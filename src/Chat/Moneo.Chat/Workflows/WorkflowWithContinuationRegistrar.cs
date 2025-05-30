namespace Moneo.Chat.Workflows;

public interface IWorkflowWithContinuationRegistrar
{
    void RegisterWorkflow<TWorkflow>(ChatState state, string commandKey) 
        where TWorkflow : IWorkflowManagerWithContinuation;
}

internal class WorkflowWithContinuationRegistrar : IWorkflowWithContinuationRegistrar
{
    private readonly ICommandStateRegistry _registry;
    private readonly Dictionary<ChatState, Type> _workflowTypes = new();

    public WorkflowWithContinuationRegistrar(ICommandStateRegistry registry)
    {
        _registry = registry;
    }

    public void RegisterWorkflow<TWorkflow>(ChatState state, string commandKey) 
        where TWorkflow : IWorkflowManagerWithContinuation
    {
        _registry.RegisterCommand(state, commandKey);
        _workflowTypes[state] = typeof(TWorkflow);
    }
    
    public Type? GetWorkflowTypeForState(ChatState state) =>
        _workflowTypes.GetValueOrDefault(state);
}
