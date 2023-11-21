namespace Moneo.Models.Chat;

public record User(Guid Id, string FirstName, string? LastName, long ConversationId);