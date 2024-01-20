namespace Moneo.Chat.Models;

public class User(Guid id, string firstName, string? lastName, long conversationId)
{
    public Guid Id { get; private set; } = id;
    public string Firstname { get; private set; } = firstName;
    public string? Lastname { get; private set; } = lastName;
    public long ConversationId { get; private set; } = conversationId;
}