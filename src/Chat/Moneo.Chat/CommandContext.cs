using Moneo.Chat.Models;
using Moneo.Chat.Workflows.Chitchat;

namespace Moneo.Chat;

public static class CommandContextFactory
{
    private static readonly CommandStateRegistry CommandStateRegistry = new ();
    
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
    
    public static CommandContext BuildCommandContext(long conversationId, long forUserId, ChatState state, string text)
    {
        var context = new CommandContext
        {
            ConversationId = conversationId,
            CurrentState = state,
            ForUserId = forUserId
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
    public long ForUserId { get; set; } = 0;

    internal CommandContext()
    {
    }
}