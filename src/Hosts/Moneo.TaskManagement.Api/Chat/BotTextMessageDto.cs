using Moneo.Chat;

namespace Moneo.TaskManagement.Api.Chat;

internal record BotTextMessageDto(long ConversationId, string Text, bool IsError = false) : IBotTextMessage;
internal record BotGifMessageDto(long ConversationId, string GifUrl) : IBotGifMessage;