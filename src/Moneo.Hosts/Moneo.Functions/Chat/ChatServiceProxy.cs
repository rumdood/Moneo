using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Moneo.Functions.Chat;

internal class ChatServiceProxyConfiguration
{
    public string EndpointUrl { get; init; }
    public string ApiKey { get; init; }
}

internal interface IChatServiceProxy
{
    Task SendTextMessageToUserAsync(long chatId, string message);
    Task SendGifMessageToUserAsync(long chatId, string gifUrl);
}

internal class ChatServiceProxy: IChatServiceProxy
{
    private readonly RestClient _client;
    private readonly ILogger<ChatServiceProxy> _logger;
    private readonly ChatServiceProxyConfiguration _configuration;
    private const string FunctionKeyHeader = "x-functions-key";

    public ChatServiceProxy(ILogger<ChatServiceProxy> logger, ChatServiceProxyConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var options = new RestClientOptions(_configuration.EndpointUrl);
        _client = new RestClient(options);
    }

    private RestRequest CreateNewRequest(string resource)
    {
        var request = new RestRequest(resource);
        request.AddHeader(FunctionKeyHeader, _configuration.ApiKey);

        return request;
    }

    private async Task SendMessageToUserAsync(long chatId, string endpoint, string data)
    {
        var request = CreateNewRequest($"{chatId}/send/{endpoint}");
        request.AddJsonBody(data);
        var response = await _client.PostAsync(request);

        if (!response.IsSuccessful)
        {
            _logger.LogError("Failed to send message to {@ChatId} - {@ErrorMessage}", chatId, response.ErrorMessage);
        }
    }

    public Task SendTextMessageToUserAsync(long chatId, string message) =>
        SendMessageToUserAsync(chatId, "text", message);

    public Task SendGifMessageToUserAsync(long chatId, string gifUrl) =>
        SendMessageToUserAsync(chatId, "gif", gifUrl);
}