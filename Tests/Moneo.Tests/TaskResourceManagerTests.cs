using AutoFixture;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moneo.Core;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client;
using Moneo.Obsolete.TaskManagement.Client.Models;
using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Tests;

public class TaskResourceManagerTests
{
    private class MockCacheItem : ICacheEntry
    {
        public void Dispose()
        {
            return;
        }

        public object Key { get; init; }
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; }
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }
    }
    private class MockMemoryCache : IMemoryCache
    {
        private readonly Dictionary<object, ICacheEntry> _cache = new();
        
        public void Dispose()
        {
            _cache.Clear();
        }

        public bool TryGetValue(object key, out object? value)
        {
            if (_cache.TryGetValue(key, out var cacheEntry))
            {
                value = cacheEntry.Value;
                return true;
            }

            value = null;
            return false;
        }

        public ICacheEntry CreateEntry(object key)
        {
            _cache[key] = new MockCacheItem {Key = key};
            return _cache[key];
        }

        public void Remove(object key)
        {
            throw new NotImplementedException();
        }
    }
    
    private const long ConversationId = 100;
    private const string Thing1 = "dothing1";
    private const string Thing2 = "dothing2";
    private const string Thing3 = "askforsomething";
    
    private readonly Fixture _fixture;
    private readonly IMemoryCache _cache;
    private readonly Mock<ITaskManagerClient> _client;
    private readonly Dictionary<long, Dictionary<string, MoneoTaskDto>> _conversationCache = new();
    private readonly Dictionary<string, MoneoTaskManagerDto> _topCache = new();

    private void InitData()
    {
        _conversationCache[ConversationId] = new Dictionary<string, MoneoTaskDto>
        {
            {Thing1, new MoneoTaskDto {Name = Thing1, Description = "Do Thing 1"}},
            {Thing2, new MoneoTaskDto {Name = Thing2, Description = "Do the 2 thing"}},
            {Thing3, new MoneoTaskDto {Name = Thing3, Description = "Ask for something to be done"}}
        };

        var tasks = _conversationCache[ConversationId];
        foreach (var kvp in tasks)
        {
            var id = new TaskFullId(ConversationId.ToString(), kvp.Key);
            _topCache[id.FullId] =
                new MoneoTaskManagerDto(new MoneoTaskState {Name = kvp.Key, Description = kvp.Value.Description},
                    ConversationId);
        }
    }

    public TaskResourceManagerTests()
    {
        InitData();
        
        _fixture = new Fixture();
        _cache = _fixture.Create<MockMemoryCache>();
        _client = new Mock<ITaskManagerClient>();
        _client.Setup(c => c.GetTasksForConversation(It.IsAny<long>()))
            .ReturnsAsync((long conversationId) =>
                new MoneoTaskResult<Dictionary<string, MoneoTaskDto>>(true, _conversationCache[conversationId]));
        _client.Setup(c => c.GetAllTasksAsync())
            .ReturnsAsync(new MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>(true, _topCache));
    }

    [Fact]
    public async Task FuzzyMatching_WithVerySpecificSearch_FindsSingleResult()
    {
        var mgr = _fixture.Build<TaskResourceManager>()
            .FromFactory(() => new TaskResourceManager(_client.Object, _cache, null))
            .Create();
        
        const string searchString = "ask for something";

        var matches =
            await mgr.GetTasksForUserAsync(ConversationId, new MoneoTaskFilter {SearchString = searchString});
        
        Assert.True(matches.IsSuccessful);
        Assert.Single(matches.Result);
        Assert.Collection(matches.Result,
            item => Assert.Equal(Thing3, item.Name));
    }
    
    [Fact]
    public async Task FuzzyMatching_WithGeneralSearch_FindsMultipleResults()
    {
        var mgr = _fixture.Build<TaskResourceManager>()
            .FromFactory(() => new TaskResourceManager(_client.Object, _cache, null))
            .Create();
        
        const string searchString = "thing";

        var matches =
            await mgr.GetTasksForUserAsync(ConversationId, new MoneoTaskFilter {SearchString = searchString});
        
        Assert.True(matches.IsSuccessful);
        Assert.Collection(matches.Result,
            item => Assert.Equal(Thing1, item.Name), 
            item => Assert.Equal(Thing2, item.Name),
            item => Assert.Equal(Thing3, item.Name));
    }
}