namespace Moneo.Chat;

public interface IBotTextMessage
{
    long ConversationId { get; }
    string Text { get; }
    bool IsError { get; }
}