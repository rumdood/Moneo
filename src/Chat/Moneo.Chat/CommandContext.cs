using Moneo.Chat.Models;
using Moneo.Chat.Workflows.Chitchat;

namespace Moneo.Chat;

public static class CommandContextFactory
{
    private static readonly CommandStateRegistry CommandStateRegistry = CommandStateRegistry.Instance;
    
    private static string GetCommandKeyForDefaultState(string text)
    {
        var firstWord = text.Split(' ')[0].ToLowerInvariant();
        var possibleMatch = UserRequestFactory.GetPotentialUserCommand(firstWord);
        
        if (possibleMatch == null)
        {
            return ChitChatRequest.CommandKey;
        }

        return ConfirmCommandRequest.CommandKey;
    }
    
    public static void RegisterDefaultCommandKey(ChatState state, string commandKey)
    {
        CommandStateRegistry.RegisterCommand(state, commandKey);
    }
    
    public static CommandContext BuildCommandContext(long conversationId, ChatUser? user, ChatState state, string text)
    {
        var context = new CommandContext
        {
            ConversationId = conversationId,
            CurrentState = state,
            User = user
        };

        if (text.StartsWith('/'))
        {
            var parts = text.Split(' ');
            context.CommandKey = parts[0].ToLowerInvariant();
            context.Args = parts[1..];

            return context;
        }

        context.CommandKey = CommandStateRegistry.GetCommandForState(state) ?? GetCommandKeyForDefaultState(text);
        context.Args = [text];

        return context;
    }
}

public class CommandContext
{
    public long ConversationId { get; init; }
    public string CommandKey { get; set; } = default!;
    public string[] Args { get; set; } = [];
    public ChatState CurrentState { get; set; }
    public ChatUser? User { get; set; }

    internal CommandContext()
    {
    }
}