namespace Moneo.Chat;

public static class UserRequestFactory
{
    private static readonly Dictionary<string, Func<CommandContext, IUserRequest>> Lookup = new(StringComparer.OrdinalIgnoreCase);
    
    public static void RegisterCommand(string commandKey, Func<CommandContext, IUserRequest> constructor)
    {
        Lookup.TryAdd(commandKey, constructor);
    }

    public static IUserRequest? GetUserRequest(CommandContext context)
    {
        return !Lookup.TryGetValue(context.CommandKey, out var constructor)
            ? null
            : constructor.Invoke(context);
    }

    public static string? GetPotentialUserCommand(string key)
    {
        var possibleCommand = "/" + key;
        return !Lookup.TryGetValue(possibleCommand, out _) ? null : possibleCommand;
    }
}
