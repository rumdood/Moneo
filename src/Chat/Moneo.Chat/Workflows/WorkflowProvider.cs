using Microsoft.Extensions.DependencyInjection;

namespace Moneo.Chat.Workflows;

public interface IWorkflowProvider
{
    IWorkflowManagerWithContinuation GetWorkflowManagerForState(ChatState state);
    IWorkflowManagerWithContinuation GetWorkflowManagerForCommand(string commandKey);
}

public class WorkflowProvider : IWorkflowProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandStateRegistry _registry;

    public WorkflowProvider(IServiceProvider serviceProvider, ICommandStateRegistry registry)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }

    public IWorkflowManagerWithContinuation GetWorkflowManagerForState(ChatState state)
    {
        var workflowType = _registry.GetCommandForState(state);
        if (workflowType == null)
        {
            throw new InvalidOperationException($"No workflow registered for state: {state}");
        }

        return (IWorkflowManagerWithContinuation)_serviceProvider.GetRequiredService(Type.GetType(workflowType)!);
    }

    public IWorkflowManagerWithContinuation GetWorkflowManagerForCommand(string commandKey)
    {
        var state = _registry.GetStateForCommand(commandKey);
        if (state == null)
        {
            throw new InvalidOperationException($"No state registered for command: {commandKey}");
        }

        return GetWorkflowManagerForState(state);
    }
}