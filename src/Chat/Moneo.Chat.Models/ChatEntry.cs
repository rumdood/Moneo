namespace Moneo.Chat.Models;

public class ChatEntry(
    long conversationId,
    ChatUser forChatUser,
    string message,
    MessageDirection direction,
    DateTimeOffset timeStamp)
{
    public long ConversationId { get; private set; } = conversationId;
    public ChatUser ForChatUser { get; private set; } = forChatUser;
    public string Message { get; private set; } = message;
    public MessageDirection Direction { get; private set; } = direction;
    public DateTimeOffset TimeStamp { get; private set; } = timeStamp;
}