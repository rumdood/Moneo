using Moneo.Chat.Models;

namespace Moneo.Chat;

public abstract class UserRequestBase : IUserRequest
{
    public CommandContext Context { get; }
    
    protected UserRequestBase(CommandContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected UserRequestBase(long conversationId, ChatUser? user, params string[] args)
    {
        Context = new CommandContext
        {
            Args = args,
            ConversationId = conversationId,
            User = user
        };
    }
}
