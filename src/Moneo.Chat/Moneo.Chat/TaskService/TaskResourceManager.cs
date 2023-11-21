using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Exceptions;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client.Models;
using Moneo.TaskManagement.Models;

namespace Moneo.Chat;

public class TaskResourceManager : ITaskResourceManager
{
    private readonly ITaskManagerClient _proxy;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TaskResourceManager> _logger;
    private bool _isInitialized = false;

    public TaskResourceManager(ITaskManagerClient proxy, IMemoryCache cache, ILogger<TaskResourceManager> logger)
    {
        _proxy = proxy;
        _cache = cache;
        _logger = logger;
    }

    private async Task RefreshAllTasksAsync()
    {
        var conversationStore = await GetAllTasksStoreAsync();

        if (conversationStore is null)
        {
            throw new TaskManagementException("Failed to get all tasks");
        }
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
        };
        

        foreach (var key in conversationStore.Keys)
        {
            _cache.Set(key, conversationStore[key], cacheOptions);
        }
    }

    private async Task<ConversationTaskStore?> GetAllTasksStoreAsync()
    {
        var (isSuccessful, fullDictionary, errorMessage) = await _proxy.GetAllTasksAsync();

        if (!isSuccessful)
        {
            throw new TaskManagementException("Failed to get all tasks",
                new HttpRequestException(errorMessage));
        }
        
        var store = new ConversationTaskStore();

        foreach (var key in fullDictionary.Keys)
        {
            if (!key.IsValidTaskFullId())
            {
                continue;
            }
            
            var fullId = TaskFullId.CreateFromFullId(key);
            var dto = fullDictionary[key];

            if (!store.TryGetValue(dto.ChatId, out var taskLookup))
            {
                store[dto.ChatId] = new Dictionary<string, MoneoTaskDto>();
            }

            store[dto.ChatId][fullId.TaskId] = dto.Task.ToMoneoTaskDto();
        }

        return store;
    }

    private async Task UpdateCacheForConversationAsync(long conversationId)
    {
        var (isSuccessful, taskLookup, errorMessage) = await _proxy.GetTasksForConversation(conversationId);

        if (!isSuccessful)
        {
            throw new TaskManagementException($"Failed to retrieve tasks for conversation: [{conversationId}",
                new HttpRequestException(errorMessage));
        }
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
        };

        _cache.Set(conversationId, taskLookup, cacheOptions);
    }

    private async Task<Dictionary<string, MoneoTaskDto>?> GetTaskLookupForConversationAsync(long conversationId)
    {
        if (!_cache.TryGetValue(conversationId, out _))
        {
            await UpdateCacheForConversationAsync(conversationId);
        }

        return _cache.Get<Dictionary<string, MoneoTaskDto>>(conversationId);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }
        
        await GetAllTasksStoreAsync();
        _isInitialized = true;
    }
    
    public async Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetAllTasksForUserAsync(long conversationId)
    {
        var items = await GetTaskLookupForConversationAsync(conversationId);

        if (items is null)
        {
            return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(false, Enumerable.Empty<MoneoTaskDto>(),
                "Conversation not found");
        }

        return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true, items.Values);
    }

    public Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskId) =>
        _proxy.CompleteTaskAsync(conversationId, taskId);

    public Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskId) =>
        _proxy.SkipTaskAsync(conversationId, taskId);
}
