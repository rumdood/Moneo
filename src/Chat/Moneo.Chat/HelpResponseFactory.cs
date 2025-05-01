using System.Text;

namespace Moneo.Chat;

public static class HelpResponseFactory
{
    private static readonly Dictionary<string, string> HelpResponseLookup = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> CommandList = [];
    
    public static string GetDefaultHelpResponse()
    {
        var defaultHelpText = new StringBuilder();
        defaultHelpText.AppendLine("Available Commands");
        defaultHelpText.AppendLine("------------------");
        defaultHelpText.AppendLine();
        foreach (var command in CommandList)
        {
            defaultHelpText.AppendLine(command);
        }
        
        return defaultHelpText.ToString();
    }
    
    public static void RegisterCommand(string commandKey, string helpResponse)
    {
        if (!HelpResponseLookup.TryAdd(commandKey, helpResponse))
        {
            throw new InvalidOperationException($"Command '{commandKey}' is already registered.");
        }
        
        CommandList.Add(commandKey);
    }

    public static string? GetHelpResponse(string commandKey)
    {
        return HelpResponseLookup.GetValueOrDefault(commandKey);
    }
}
