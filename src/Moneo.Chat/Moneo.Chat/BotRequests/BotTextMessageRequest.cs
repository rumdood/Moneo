using MediatR;

namespace Moneo.Chat.BotRequests;

public record BotTextMessageRequest(long ConversationId, string Text, bool IsError = false) : IRequest;

public record BotGifMessageRequest(long ConversationId, string GifUrl) : IRequest;