namespace Moneo.Bot;

public record User(Guid Id, string FirstName, string? LastName, long ConversationId);