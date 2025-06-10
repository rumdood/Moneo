using MediatR;

namespace Moneo.Chat.BotRequests;

public enum TextFormat
{
    Plain,
    Markdown,
    Html
}

public record BotTextMessageRequest(long ConversationId, string Text, TextFormat Format = TextFormat.Plain, bool IsError = false) : IBotTextMessage, IRequest;

public record BotGifMessageRequest(long ConversationId, string GifUrl) : IBotGifMessage, IRequest;

public record BotMenuMessageRequest(long ConversationId, string Text, IEnumerable<string> MenuOptions) : IRequest;

public static class TextFormatExtensions
{
    public static Moneo.Chat.BotRequests.TextFormat ToBotRequestFormat(this Moneo.Chat.Commands.TextFormat format)
    {
        return format switch
        {
            Moneo.Chat.Commands.TextFormat.Plain => TextFormat.Plain,
            Moneo.Chat.Commands.TextFormat.Markdown => TextFormat.Markdown,
            Moneo.Chat.Commands.TextFormat.Html => TextFormat.Html,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
    
    public static Moneo.Chat.Commands.TextFormat ToMoneoCommandFormat(this Moneo.Chat.BotRequests.TextFormat format)
    {
        return format switch
        {
            Moneo.Chat.BotRequests.TextFormat.Plain => Moneo.Chat.Commands.TextFormat.Plain,
            Moneo.Chat.BotRequests.TextFormat.Markdown => Moneo.Chat.Commands.TextFormat.Markdown,
            Moneo.Chat.BotRequests.TextFormat.Html => Moneo.Chat.Commands.TextFormat.Html,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}
