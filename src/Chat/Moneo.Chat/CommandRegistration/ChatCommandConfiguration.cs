using System.Reflection;

namespace Moneo.Chat.CommandRegistration;

public class MoneoChatCommandConfiguration
{
    internal List<Assembly> ChatCommandAssemblies { get; } = [];
    internal List<Type> UserRequestsToRegister { get; } = [];
    
    public MoneoChatCommandConfiguration RegisterUserRequestsFromAssemblyContaining<T>()
        => RegisterUserRequestsFromAssemblyContaining(typeof(T));

    public MoneoChatCommandConfiguration RegisterUserRequestsFromAssemblyContaining(Type type)
        => RegisterUserRequestsFromAssembly(type.Assembly);
    
    public MoneoChatCommandConfiguration RegisterUserRequestsFromAssembly(Assembly assembly)
    {
        ChatCommandAssemblies.Add(assembly);
        return this;
    }
    
    public MoneoChatCommandConfiguration RegisterUserRequestsFromAssemblies(params Assembly[] assemblies)
    {
        ChatCommandAssemblies.AddRange(assemblies);
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
}
