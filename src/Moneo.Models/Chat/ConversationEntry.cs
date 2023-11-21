namespace Moneo.Models.Chat;

public record ConversationEntry(
    long ConversationId, 
    User ForUser, 
    string Message, 
    MessageDirection Direction, 
    DateTimeOffset TimeStamp);