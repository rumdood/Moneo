using System.Reflection;

namespace Moneo.Chat.CommandRegistration;

public static class CommandRegistrar
{
    public const string RegistrationMethodName = "Register";
    
    public static void RegisterCommands(
        MoneoChatCommandConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var assemblies = configuration.ChatCommandAssemblies.Distinct().ToArray();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(IUserRequest).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                var userCommandAttribute = type.GetCustomAttribute<UserCommandAttribute>();
                if (userCommandAttribute == null)
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
        }
    }
}
