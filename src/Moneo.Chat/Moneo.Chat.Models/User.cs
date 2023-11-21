namespace Moneo.Chat.Models;

public record User(Guid Id, string FirstName, string? LastName, long ConversationId);