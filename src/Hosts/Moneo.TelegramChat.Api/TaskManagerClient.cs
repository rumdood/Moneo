using Moneo.Core;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client.Models;
using Moneo.Obsolete.TaskManagement.Models;
using RestSharp;

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
    
    public Task<MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>> GetAllTasksAsync()
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult<Dictionary<string, MoneoTaskDto>>> GetTasksForConversation(long conversationId)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskName)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskName)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult> DisableTaskAsync(long conversationId, string taskName)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoTaskResult> UpdateTaskAsync(long conversationId, string taskName, MoneoTaskDto task)
    {
        throw new NotImplementedException();
    }
}