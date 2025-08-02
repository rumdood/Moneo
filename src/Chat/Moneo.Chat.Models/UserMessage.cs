namespace Moneo.Chat.Models;

public class UserMessage(long conversationId, ChatUser? fromUser, string text)
{
    public long ConversationId { get; private set; } = conversationId;
    public string Text { get; private set; } = text;
    public ChatUser? FromUser { get; private set; } = fromUser;
}