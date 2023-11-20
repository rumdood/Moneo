using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moneo.Models;
using Newtonsoft.Json;
using RestSharp;

namespace Moneo.Bot;

internal record MoneoTaskManagerDto(MoneoTaskState Task, long ChatId);
internal class ConversationTaskStore : Dictionary<long, Dictionary<string, MoneoTaskDto>> { }

internal interface IMoneoProxy
{
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>> GetAllTasksAsync();
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskDto>>> GetTasksForConversation(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskName);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskName);
}

internal class MoneoProxy : IMoneoProxy
{
    private enum TaskAction
    {
        Complete,
        Skip
    }

    private readonly BotClientConfiguration _configuration;
    private readonly RestClient _client;
    private readonly ILogger<MoneoProxy> _logger;
    private const string FunctionKeyHeader = "x-functions-key";

    public MoneoProxy(IOptions<BotClientConfiguration> configuration, ILogger<MoneoProxy> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;

        var options = new RestClientOptions(_configuration.ApiBase);
        _client = new RestClient(options);
    }

    private async Task<MoneoTaskResult> ExecuteTaskFunctionAsync(long conversationId, string taskName,
        TaskAction action)
    {
        var actionString = action is TaskAction.Complete ? "complete" : "skip";

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
}
