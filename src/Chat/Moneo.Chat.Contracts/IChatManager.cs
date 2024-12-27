using Moneo.Chat.Models;

namespace Moneo.Chat;

public interface IChatManager
{
    IEnumerable<ChatEntry> GetLastEntriesForConversation(long conversationId, int count);
    Task ProcessUserMessageAsync(UserMessage message);
    bool AddUser(long conversationId, string firstName, string? lastName);
    Task<ChatState> GetChatStateForConversationAsync(long conversationId);
}