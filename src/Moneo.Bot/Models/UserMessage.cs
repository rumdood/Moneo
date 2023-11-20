namespace Moneo.Bot;

internal record UserMessage(long ConversationId, string Text, string UserFirstName, string? UserLastName = null);