using System.Reflection;

namespace Moneo.Chat.CommandRegistration;

public static class CommandRegistrar
{
    private static readonly CommandStateRegistry CommandStateRegistry = CommandStateRegistry.Instance;
    public const string RegistrationMethodName = "Register";
    
    public static void RegisterCommands(
        MoneoChatCommandConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var assemblies = configuration.MoneoRegistrationAssemblies.Distinct().ToArray();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(IUserRequest).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                var customAttributes = type.GetCustomAttributes()
                    .Where(attr => attr is UserCommandAttribute or WorkflowContinuationCommandAttribute)
                    .ToArray();
                
                if (customAttributes.Length == 0)
                {
                    continue;
                }

                configuration.UserRequestsToRegister.Add(type);
            }
        }

        foreach (var type in configuration.UserRequestsToRegister.Distinct())
        {
            var registerMethod = type.GetMethod(RegistrationMethodName, BindingFlags.Public | BindingFlags.Static);
            registerMethod?.Invoke(null, null);
            
            var continuationAttribute = type.GetCustomAttributes<WorkflowContinuationCommandAttribute>().SingleOrDefault();

            if (continuationAttribute is null)
            {
                continue;
            }
                
            // register the continuation state and command
            CommandStateRegistry.RegisterCommand(
                ChatState.FromName(continuationAttribute.RequiredChatStateName),
                continuationAttribute.CommandKey);
        }
    }
}
