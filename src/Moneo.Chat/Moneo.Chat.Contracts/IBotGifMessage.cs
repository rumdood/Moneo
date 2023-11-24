namespace Moneo.Chat;

public interface IBotGifMessage
{
    long ConversationId { get; }
    string GifUrl { get; }
}