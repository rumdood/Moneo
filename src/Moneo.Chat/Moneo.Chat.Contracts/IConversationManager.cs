using Moneo.Chat.Models;

namespace Moneo.Chat;

public interface IConversationManager
{
    IEnumerable<ConversationEntry> GetLastEntriesForConversation(long conversationId, int count);
    Task ProcessUserMessageAsync(UserMessage message);
    bool AddUser(long conversationId, string firstName, string? lastName);
}