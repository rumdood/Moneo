using Moneo.Chat.Workflows.ChangeTask;
using Moneo.Chat.Workflows.Chitchat;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat;

public class CommandContext
{
    public long ConversationId { get; init; }
    public string CommandKey { get; set; } = default!;
    public string[] Args { get; set; } = [];
    public ChatState CurrentState { get; set; }

    private CommandContext()
    {   
    }

    public static CommandContext Get(long conversationId, ChatState state, string text)
    {
        var context = new CommandContext
        {
            ConversationId = conversationId,
            CurrentState = state,
        };

        if (text.StartsWith('/'))
        {
            var parts = text.Split(' ');
            context.CommandKey = parts[0].ToLowerInvariant();
            context.Args = parts[1..];

            return context;
        }

        context.CommandKey = state switch
        {
            ChatState.CreateTask => CreateTaskContinuationRequest.CommandKey,
            ChatState.ChangeTask => ChangeTaskContinuationRequest.CommandKey,
            ChatState.CreateCron => CreateCronContinuationRequest.CommandKey,
            ChatState.ConfirmCommand => ConfirmCommandContinuationRequest.CommandKey,
            _ => GetCommandKeyForDefaultState(text)
        };
        context.Args = [text];

        return context;
    }

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
}