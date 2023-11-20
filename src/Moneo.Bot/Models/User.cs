namespace Moneo.Bot;

internal record User(Guid Id, string FirstName, string? LastName, long ConversationId);