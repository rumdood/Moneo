using Moneo.Chat.Models;

namespace Moneo.Chat;

public abstract class UserRequestBase : IUserRequest
{
    public long ConversationId { get; protected set; }
    public long ForUserId { get; protected set; } = 0; // Default to 0, meaning no specific user is targeted
    protected string[] Args;

    protected UserRequestBase(long conversationId, ChatUser? user, params string[] args)
    {
        ConversationId = conversationId;
        Args = args;
        if (user != null)
        {
            ForUserId = user.Id;
        }
    }
}
