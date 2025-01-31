using Moneo.Common;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;
using RestSharp;
using IBotClientConfiguration = Moneo.Core.IBotClientConfiguration;

namespace Moneo.TelegramChat.Api;

public class TaskManagerClient : ITaskManagerClient
{
    private readonly IBotClientConfiguration _configuration;
    private readonly RestClient _client;
    private readonly ILogger<TaskManagerClient> _logger;

    public TaskManagerClient(IBotClientConfiguration configuration, ILogger<TaskManagerClient> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var options = new RestClientOptions(_configuration.TaskApiBase);
        _client = new RestClient(options);
    }

    public Task<Common.MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForConversationAsync(long conversationId, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAsync(long userId, PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAndConversationAsync(long userId, long conversationId, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult<PagedList<MoneoTaskDto>>> GetTasksByKeywordSearchAsync(long conversationId, string keyword, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult<MoneoTaskDto>> GetTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult<MoneoTaskDto>> CreateTaskAsync(long conversationId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult> UpdateTaskAsync(long taskId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult> CompleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult> SkipTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Common.MoneoResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}