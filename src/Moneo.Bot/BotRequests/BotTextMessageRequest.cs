using MediatR;

namespace Moneo.Chat.BotRequests;

internal record BotTextMessageRequest(long ConversationId, string Text, bool IsError = false) : IRequest;
internal record BotGifMessageRequest(long ConversationId, string GifUrl) : IRequest;