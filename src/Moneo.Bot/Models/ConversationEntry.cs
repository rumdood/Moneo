namespace Moneo.Bot;

internal record ConversationEntry(
    long ConversationId, 
    User ForUser, 
    string Message, 
    MessageDirection Direction, 
    DateTimeOffset TimeStamp);