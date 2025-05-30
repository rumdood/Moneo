using System.Reflection;
using Moneo.Chat.Workflows;

namespace Moneo.Chat.CommandRegistration;

public class MoneoChatCommandConfiguration
{
    internal List<Assembly> MoneoRegistrationAssemblies { get; } = [];
    internal List<Type> UserRequestsToRegister { get; } = [];
    internal List<Type> WorkflowManagersToRegister { get; } = [];
    
    public MoneoChatCommandConfiguration RegisterUserRequestsAndWorkflowsFromAssemblyContaining<T>()
        => RegisterUserRequestsAndWorkflowsFromAssemblyContaining(typeof(T));

    public MoneoChatCommandConfiguration RegisterUserRequestsAndWorkflowsFromAssemblyContaining(Type type)
        => RegisterUserRequestsAndWorkflowsFromAssembly(type.Assembly);
    
    public MoneoChatCommandConfiguration RegisterUserRequestsAndWorkflowsFromAssembly(Assembly assembly)
    {
        MoneoRegistrationAssemblies.Add(assembly);
        return this;
    }
    
    public MoneoChatCommandConfiguration RegisterUserRequestsAndWorkflowsFromAssemblies(params Assembly[] assemblies)
    {
        MoneoRegistrationAssemblies.AddRange(assemblies);
        return this;
    }
    
    public MoneoChatCommandConfiguration RegisterUserRequest(Type userRequest)
    {
        if (!typeof(IUserRequest).IsAssignableFrom(userRequest) ||
            !typeof(UserRequestBase).IsAssignableFrom(userRequest) || 
            userRequest.IsAbstract)
        {
            throw new ArgumentException($"Type {userRequest.Name} is not a valid user request type.");
        }

        UserRequestsToRegister.Add(userRequest);
        return this;
    }

    public MoneoChatCommandConfiguration RegisterWorkflowManager(Type workflowManager)
    {
        // check to see if the type has the custom attribute MoneoWorkflowAttribute
        if (workflowManager.IsAbstract || 
            workflowManager.GetCustomAttribute<MoneoWorkflowAttribute>() is null)
        {
            throw new ArgumentException($"Type {workflowManager.Name} is not a valid workflow manager type.");
        }
        WorkflowManagersToRegister.Add(workflowManager);
        return this;
    }
}
