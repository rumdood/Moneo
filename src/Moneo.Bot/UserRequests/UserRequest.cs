namespace Moneo.Chat.UserRequests;

public interface IUserRequest
{
    long ConversationId { get; }
    string Name { get; }
}
