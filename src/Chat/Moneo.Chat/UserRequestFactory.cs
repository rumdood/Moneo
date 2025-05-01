namespace Moneo.Chat;

public static class UserRequestFactory
{
    private static readonly Dictionary<string, Func<long, string[], IUserRequest>> _lookup = new(StringComparer.OrdinalIgnoreCase);
    
    public static void RegisterCommand(string commandKey, Func<long, string[], IUserRequest> constructor)
    {
        _lookup.TryAdd(commandKey, constructor);
    }

    public static IUserRequest? GetUserRequest(CommandContext context)
    {
        return !_lookup.TryGetValue(context.CommandKey, out var constructor)
            ? null
            : constructor.Invoke(context.ConversationId, context.Args);
    }

    public static string? GetPotentialUserCommand(string key)
    {
        var possibleCommand = "/" + key;
        return !_lookup.TryGetValue(possibleCommand, out _) ? null : possibleCommand;
    }
}
