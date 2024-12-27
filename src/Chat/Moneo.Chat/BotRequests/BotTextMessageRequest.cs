using MediatR;

namespace Moneo.Chat.BotRequests;

public record BotTextMessageRequest(long ConversationId, string Text, bool IsError = false) : IBotTextMessage, IRequest;

public record BotGifMessageRequest(long ConversationId, string GifUrl) : IBotGifMessage, IRequest;

public record BotMenuMessageRequest(long ConversationId, string Text, IEnumerable<string> MenuOptions) : IRequest;