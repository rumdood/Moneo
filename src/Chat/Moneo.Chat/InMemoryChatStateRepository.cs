using System.Collections;
using Microsoft.Extensions.Logging;

namespace Moneo.Chat;

public record ChatStateEntry(long ChatId, long UserId, ChatState State);

public interface IChatStateRepository
{
    Task UpdateChatStateAsync(long chatId, long userId, ChatState state);
    Task<ChatState> GetChatStateAsync(long chatId, long userId);
    Task<ChatState> RevertChatStateAsync(long chatId, long userId);
    Task<IReadOnlyList<ChatStateEntry>> GetAllChatStatesAsync();
}

internal class InMemoryChatStateRepository : IChatStateRepository
{
    private readonly ILogger<InMemoryChatStateRepository> _logger;
    private readonly Dictionary<(long, long), Stack<ChatState>> _states = new();

    public InMemoryChatStateRepository(ILogger<InMemoryChatStateRepository> logger)
    {
        _logger = logger;
    }

    private Stack<ChatState> GetChatStateHistory(long chatId, long userId) 
    {
        if (_states.TryGetValue((chatId, userId), out var stateHistory))
        {
            return stateHistory;
        }
        
        stateHistory = new Stack<ChatState>();
        stateHistory.Push(ChatState.Waiting);
        _states[(chatId, userId)] = stateHistory;

        return stateHistory;
    }

    public Task<ChatState> GetChatStateAsync(long chatId, long userId)
    {
        var stateHistory = GetChatStateHistory(chatId, userId);
        return Task.FromResult(stateHistory.First());
    }

    public Task UpdateChatStateAsync(long chatId, long userId, ChatState state)
    {
        var stateHistory = GetChatStateHistory(chatId, userId);

        if (state == ChatState.Waiting)
        {
            // reset the stack to just the waiting state
            stateHistory.Clear();
        }
        
        _logger.LogDebug("Updating chat state for chat {ChatId}, user {UserId} to {State}", chatId, userId, state);
        
        stateHistory.Push(state);
        return Task.CompletedTask;
    }

    public Task<ChatState> RevertChatStateAsync(long chatId, long userId)
    {
        var stateHistory = GetChatStateHistory(chatId, userId);

        if (stateHistory.Peek() == ChatState.Waiting)
        {
            // we're in a waiting state, you don't revert that
            return Task.FromResult(ChatState.Waiting);
        }

        _ = stateHistory.Pop();
        var previousState = stateHistory.Peek();
        _logger.LogDebug("Reverted chat state for chat {ChatId}, user {UserId} to {State}", chatId, userId, previousState);
        return Task.FromResult(previousState);
    }
    
    public Task<IReadOnlyList<ChatStateEntry>> GetAllChatStatesAsync()
    {
        var entries = _states.Select(kvp => new ChatStateEntry(kvp.Key.Item1, kvp.Key.Item2, kvp.Value.Peek()))
            .ToList();
        return Task.FromResult<IReadOnlyList<ChatStateEntry>>(entries);
    }
}
