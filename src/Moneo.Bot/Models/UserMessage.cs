namespace Moneo.Bot;

public record UserMessage(long ConversationId, string Text, string UserFirstName, string? UserLastName = null);