namespace Moneo.Chat.Models;

public class ChatEntry(
    long conversationId,
    User forUser,
    string message,
    MessageDirection direction,
    DateTimeOffset timeStamp)
{
    public long ConversationId { get; private set; } = conversationId;
    public User ForUser { get; private set; } = forUser;
    public string Message { get; private set; } = message;
    public MessageDirection Direction { get; private set; } = direction;
    public DateTimeOffset TimeStamp { get; private set; } = timeStamp;
}