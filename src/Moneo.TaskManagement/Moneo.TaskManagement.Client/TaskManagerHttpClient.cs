using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.TaskManagement.Client.Models;
using Moneo.TaskManagement.Models;
using Newtonsoft.Json;
using RestSharp;

namespace Moneo.TaskManagement.Client;

internal class ConversationTaskStore : Dictionary<long, Dictionary<string, MoneoTaskDto>> { }

internal enum TaskAction
{
    Complete,
    Skip,
    Disable,
}

public class TaskManagerHttpClient : ITaskManagerClient
{
    private readonly IBotClientConfiguration _configuration;
    private readonly RestClient _client;
    private readonly ILogger<TaskManagerHttpClient> _logger;
    private const string FunctionKeyHeader = "x-functions-key";

    public TaskManagerHttpClient(IBotClientConfiguration configuration, ILogger<TaskManagerHttpClient> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var options = new RestClientOptions(_configuration.TaskApiBase);
        _client = new RestClient(options);
    }

    private async Task<MoneoTaskResult> ExecuteTaskFunctionAsync(long conversationId, string taskName,
        TaskAction action)
    {
        var actionString = action switch
        {
            TaskAction.Complete => "complete",
            TaskAction.Skip => "skip",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        var request = new RestRequest($"{conversationId}/tasks/{taskName}/{actionString}");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);

        var response = await _client.PostAsync(request);

        if (!response.IsSuccessful)
        {
            _logger.LogError("Failed to {@Action} task: {@Error}", actionString, response.ErrorMessage);
        }

        return new MoneoTaskResult(response.IsSuccessful, response.ErrorMessage);
    }

    public async Task<MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>> GetAllTasksAsync()
    {
        var request = new RestRequest("/tasks");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);

        var response = await _client.ExecuteGetAsync(request);

        if (response.IsSuccessful)
        {
            var json = response.Content!;
            var fullDictionary = JsonConvert.DeserializeObject<Dictionary<string, MoneoTaskManagerDto>>(json);

            if (fullDictionary is null)
            {
                return new MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>(false,
                    new Dictionary<string, MoneoTaskManagerDto>(), "Error while deserializing task dictionary");
            }

            return new MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>(true, fullDictionary);
        }

        _logger.LogError("Failed to retrieve task list");
        return new MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>(false,
            new Dictionary<string, MoneoTaskManagerDto>(),
            response.ErrorMessage);
    }

    public async Task<MoneoTaskResult<Dictionary<string, MoneoTaskDto>>> GetTasksForConversation(long conversationId)
    {
        var request = new RestRequest($"{conversationId}/tasks");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);

        var response = await _client.ExecuteGetAsync<Dictionary<string, MoneoTaskDto>>(request);

        if (response.IsSuccessful)
        {
            return new MoneoTaskResult<Dictionary<string, MoneoTaskDto>>(true, response.Data!);
        }

        _logger.LogError("Failed to retrieve task list");
        return new MoneoTaskResult<Dictionary<string, MoneoTaskDto>>(false, new Dictionary<string, MoneoTaskDto>(),
            response.ErrorMessage);
    }

    public Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskName) =>
        ExecuteTaskFunctionAsync(conversationId, taskName, TaskAction.Complete);

    public Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskName) =>
        ExecuteTaskFunctionAsync(conversationId, taskName, TaskAction.Skip);

    public async Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task)
    {
        var taskName = task.Name.ToLowerInvariant().Replace(" ", "");
        var request = new RestRequest($"{conversationId}/tasks/{taskName}");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);
        request.AddJsonBody(task);

        var response = await _client.ExecutePostAsync(request);

        return new MoneoTaskResult(response.IsSuccessful, response.ErrorMessage);
    }

    public async Task<MoneoTaskResult> DisableTaskAsync(long conversationId, string taskName)
    {
        var request = new RestRequest($"{conversationId}/tasks/{taskName}");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);

        var response = await _client.DeleteAsync(request);

        if (!response.IsSuccessful)
        {
            _logger.LogError("Failed to DISABLE task: {@Error}", response.ErrorMessage);
        }

        return new MoneoTaskResult(response.IsSuccessful, response.ErrorMessage);
    }
}
