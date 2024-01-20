namespace Moneo.Chat;

public abstract class UserRequestBase : IUserRequest
{
    public long ConversationId { get; protected set; }
    protected string[] Args;

    protected UserRequestBase(long conversationId, params string[] args)
    {
        ConversationId = conversationId;
        Args = args;
    }
}
