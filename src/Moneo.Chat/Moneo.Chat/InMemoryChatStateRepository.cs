using System.Collections;

namespace Moneo.Chat;

public interface IChatStateRepository
{
    Task UpdateChatStateAsync(long chatId, ChatState state);
    Task<ChatState> GetChatStateAsync(long chatId);
    Task RevertChatStateAsync(long chatId);
}

public class InMemoryChatStateRepository : IChatStateRepository
{
    private readonly Dictionary<long, HistoryLedger<ChatState>> _states = new();

    private HistoryLedger<ChatState> GetChatStateHistory(long chatId) 
    {
        if (!_states.TryGetValue(chatId, out var stateHistory))
        {
            stateHistory = new();
            _states[chatId] = stateHistory;
            stateHistory.Add(ChatState.Waiting);
        }

        return stateHistory;
    }

    public Task<ChatState> GetChatStateAsync(long chatId)
    {
        var stateHistory = GetChatStateHistory(chatId);
        return Task.FromResult(stateHistory.Last());
    }

    public Task UpdateChatStateAsync(long chatId, ChatState state)
    {
        var stateHistory = GetChatStateHistory(chatId);
        stateHistory.Add(state);
        return Task.CompletedTask;
    }

    public Task RevertChatStateAsync(long chatId)
    {
        var stateHistory = GetChatStateHistory(chatId);

        if (stateHistory.Current() == ChatState.Waiting)
        {
            // we're in a waiting state, you don't revert that
            return Task.CompletedTask;
        }

        if (!stateHistory.TryGetPrevious(out var previousState))
        {
            // nothing to revert to
            return Task.CompletedTask;
        }

        UpdateChatStateAsync(chatId, previousState); 
        return Task.CompletedTask;
    }
}

public class HistoryLedger<T> : IEnumerable<T>
{
    private readonly Queue<T> _history = new Queue<T>();
    private const int _maxLength = 5;

    public void Add(T item)
    {
        _history.Enqueue(item);

        if (_history.Count > _maxLength)
        {
            _ = _history.Dequeue();
        }
    }

    public void Clear()
    {
        _history.Clear();
    }

    public T Current() => _history.Last();

    public T Previous()
    {
        if (_history.Count >= 2)
        {
            var array = _history.ToArray();
            return array[array.Length - 2];
        }

        return _history.First();
    }

    public bool TryGetPrevious(out T? item)
    {
        if (_history.Count >= 2)
        {
            var array = _history.ToArray();
            item = array[array.Length - 2];
            return true;
        }

        item = default;
        return false;
    }

    public IEnumerator<T> GetEnumerator() => _history.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
