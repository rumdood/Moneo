using System.Collections;

namespace Moneo.Chat;

public interface IChatStateRepository
{
    Task UpdateChatStateAsync(long chatId, ChatState state);
    Task<ChatState> GetChatStateAsync(long chatId);
    Task<ChatState> RevertChatStateAsync(long chatId);
}

internal class InMemoryChatStateRepository : IChatStateRepository
{
    private readonly Dictionary<long, Stack<ChatState>> _states = new();

    private Stack<ChatState> GetChatStateHistory(long chatId) 
    {
        if (_states.TryGetValue(chatId, out var stateHistory))
        {
            return stateHistory;
        }
        
        stateHistory = new Stack<ChatState>();
        stateHistory.Push(ChatState.Waiting);
        _states[chatId] = stateHistory;

        return stateHistory;
    }

    public Task<ChatState> GetChatStateAsync(long chatId)
    {
        var stateHistory = GetChatStateHistory(chatId);
        return Task.FromResult(stateHistory.First());
    }

    public Task UpdateChatStateAsync(long chatId, ChatState state)
    {
        var stateHistory = GetChatStateHistory(chatId);

        if (state == ChatState.Waiting)
        {
            // reset the stack to just the waiting state
            stateHistory.Clear();
        }
        
        stateHistory.Push(state);
        return Task.CompletedTask;
    }

    public Task<ChatState> RevertChatStateAsync(long chatId)
    {
        var stateHistory = GetChatStateHistory(chatId);

        if (stateHistory.Peek() == ChatState.Waiting)
        {
            // we're in a waiting state, you don't revert that
            return Task.FromResult(ChatState.Waiting);
        }

        _ = stateHistory.Pop();
        return Task.FromResult(stateHistory.Peek());
    }
}
