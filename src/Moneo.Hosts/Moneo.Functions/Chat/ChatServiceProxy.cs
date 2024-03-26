using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace Moneo.Functions.Chat;

internal interface IChatServiceProxy
{
    Task SendTextMessageToUserAsync(long chatId, string message);
    Task SendGifMessageToUserAsync(long chatId, string gifUrl);
}

internal record ChatMessage(long ConversationId, string Message, bool IsError = false);

internal class ChatServiceProxy: IChatServiceProxy
{
    private readonly RestClient _client;
    private readonly ILogger<ChatServiceProxy> _logger;
    private const string FunctionKeyHeader = "x-functions-key";

    public ChatServiceProxy(ILogger<ChatServiceProxy> logger)
    {
        _logger = logger;
        var options = new RestClientOptions(MoneoConfiguration.ChatServiceEndpoint);
        _client = new RestClient(
            MoneoConfiguration.ChatServiceEndpoint, 
            configureDefaultHeaders: cfg => cfg.Add(FunctionKeyHeader, MoneoConfiguration.ChatServiceApiKey), 
            configureSerialization: cfg => cfg.UseNewtonsoftJson());
    }

    private async Task SendMessageToUserAsync(long chatId, string endpoint, string data)
    {
        var request = new RestRequest($"send/{endpoint}");
        request.AddJsonBody(new ChatMessage(chatId, data));
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
