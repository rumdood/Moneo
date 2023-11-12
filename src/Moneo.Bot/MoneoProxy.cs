using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Moneo.Bot;

public record MoneoProxyResult(bool IsSuccessful, string? ErrorMessage);

public interface IMoneoProxy
{
    Task<MoneoProxyResult> CompleteTaskAsync(long conversationId, string taskName);
    Task<MoneoProxyResult> SkipTaskAsync(long conversationId, string taskName);
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

    private async Task<MoneoProxyResult> ExecuteTaskFunctionAsync(long conversationId, string taskName, TaskAction action)
    {
        var actionString = action is TaskAction.Complete ? "complete" : "skip";

        var request = new RestRequest($"{conversationId}/tasks/{taskName}/{actionString}");
        request.AddHeader(FunctionKeyHeader, _configuration.FunctionKey);
        
        var response = await _client.PostAsync(request);

        if (!response.IsSuccessful)
        {
            _logger.LogError("Failed to {@Action} task: {@Error}", actionString, response.ErrorMessage);
        }

        return new MoneoProxyResult(response.IsSuccessful, response.ErrorMessage);
    }

    public Task<MoneoProxyResult> CompleteTaskAsync(long conversationId, string taskName) =>
        ExecuteTaskFunctionAsync(conversationId, taskName, TaskAction.Complete);

    public Task<MoneoProxyResult> SkipTaskAsync(long conversationId, string taskName) =>
        ExecuteTaskFunctionAsync(conversationId, taskName, TaskAction.Skip);
}