using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.Core.Exceptions;
using Moneo.TaskManagement.Client.Models;
using Moneo.TaskManagement.Models;

namespace Moneo.TaskManagement.Client;

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

    public Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task) =>
        _proxy.CreateTaskAsync(conversationId, task);

    public async Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetTasksForUserAsync(long conversationId, MoneoTaskFilter filter)
    {
        var lookup = await GetTaskLookupForConversationAsync(conversationId);
        
        if (lookup is null || (string.IsNullOrEmpty(filter.TaskId) && string.IsNullOrEmpty(filter.SearchString)))
        {
            return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(false, Enumerable.Empty<MoneoTaskDto>(),
                $"No tasks were found");
        }
        
        if (!string.IsNullOrEmpty(filter.TaskId))
        {
            if (lookup.TryGetValue(filter.TaskId, out var match))
            {
                return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true, [match]);
            }

            return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(false, Enumerable.Empty<MoneoTaskDto>(),
                $"The task with an ID of {filter.TaskId} was not found");
        }

        var matches = FuzzySharp.Process.ExtractTop(filter.SearchString, lookup.Keys, cutoff: 75).ToArray();
        if (matches.Length > 0)
        {
            return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true,
                lookup
                    .Where(kv => matches.Select(x => x.Value).Contains(kv.Key))
                    .Select(kv => kv.Value));
        }

        matches = FuzzySharp.Process
            .ExtractTop(filter.SearchString, lookup.Values.Select(x => x.Description), cutoff: 75).ToArray();

        if (matches.Length > 0)
        {
            return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true,
                lookup.Values.Where(v => 
                    matches.Select(x => x.Value).Contains(v.Description)));
        }

        return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(false, Enumerable.Empty<MoneoTaskDto>(),
            $"No tasks were found for the search string \"{filter.SearchString}\"");
    }
}
