namespace Moneo.Chat;

public static partial class HelpResponseFactory
{
    private static readonly Dictionary<string, string> _lookup = new(StringComparer.OrdinalIgnoreCase);

    public static string? GetHelpResponse(string commandKey)
    {
        if (_lookup.Count == 0)
        {
            InitializeLookup();
        }

        if (!_lookup.TryGetValue(commandKey, out var response))
        {
            return null;
        }

        return response;
    }
}
