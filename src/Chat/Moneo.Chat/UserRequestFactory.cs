using Moneo.Chat.Models;

namespace Moneo.Chat;

public static class UserRequestFactory
{
    private static readonly Dictionary<string, Func<long, ChatUser?, string[], IUserRequest>> Lookup = new(StringComparer.OrdinalIgnoreCase);
    
    public static void RegisterCommand(string commandKey, Func<long, ChatUser?, string[], IUserRequest> constructor)
    {
        Lookup.TryAdd(commandKey, constructor);
    }

    public static IUserRequest? GetUserRequest(CommandContext context)
    {
        return !Lookup.TryGetValue(context.CommandKey, out var constructor)
            ? null
            : constructor.Invoke(context.ConversationId, new ChatUser(context.ForUserId, ""), context.Args);
    }

    public static string? GetPotentialUserCommand(string key)
    {
        var possibleCommand = "/" + key;
        return !Lookup.TryGetValue(possibleCommand, out _) ? null : possibleCommand;
    }
}
