namespace Moneo.Chat;

public static partial class UserRequestFactory
{
    private static readonly Dictionary<string, Func<long, string[], IUserRequest>> _lookup = new(StringComparer.OrdinalIgnoreCase);

    public static IUserRequest? GetUserRequest(CommandContext context)
    {
        if (_lookup.Count == 0)
        {
            InitializeLookup();
        }

        if (!_lookup.TryGetValue(context.CommandKey, out var constructor))
        {
            return null;
        }

        return constructor.Invoke(context.ConversationId, context.Args);
    }

    public static string? GetPotentialUserCommand(string key)
    {
        if (_lookup.Count == 0)
        {
            InitializeLookup();
        }

        var possibleCommand = "/" + key;
        if (!_lookup.TryGetValue(possibleCommand, out _))
        {
            return null;
        }

        return possibleCommand;
    }
}
