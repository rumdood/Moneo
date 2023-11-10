namespace Moneo.Bot.UserRequests;

public interface IUserRequest
{
    long ConversationId { get; }
    string Name { get; }
}
