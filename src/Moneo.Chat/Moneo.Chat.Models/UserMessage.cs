namespace Moneo.Chat.Models;

public record UserMessage(long ConversationId, string Text, string UserFirstName, string? UserLastName = null);