namespace Moneo.Chat.Models;

public class UserMessage(long conversationId, string text, string userFirstName, string? userLastName = null)
{
    public long ConversationId { get; private set; } = conversationId;
    public string Text { get; private set; } = text;
    public string UserFirstName { get; private set; } = userFirstName;
    public string? UserLastName { get; private set; } = userLastName;
}