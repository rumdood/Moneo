using Moneo.Chat.Models;

namespace Moneo.Chat;

public interface IChatManager
{
    IEnumerable<ChatEntry> GetLastEntriesForConversation(long conversationId, int count);
    Task ProcessUserMessageAsync(UserMessage message);
    Task<ChatState> GetChatStateForConversationAsync(long conversationId);
}