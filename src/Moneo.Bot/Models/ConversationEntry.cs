namespace Moneo.Bot;

public record ConversationEntry(
    long ConversationId, 
    User ForUser, 
    string Message, 
    MessageDirection Direction, 
    DateTimeOffset TimeStamp);