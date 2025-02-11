using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Moneo.Common;
using Moneo.TaskManagement.Api.Chat;

namespace Moneo.TaskManagement.Api.Services;

internal interface INotificationService
{
    Task<MoneoResult> SendTextNotification(
        long conversationId, 
        string message, 
        bool isError = false,
        CancellationToken cancellationToken = default);
}

internal class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public NotificationService(HttpClient httpClient, ILogger<NotificationService> logger, NotificationConfig config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config.ApiKey;
        _baseUrl = config.BaseUrl;
    }

    public async Task<MoneoResult> SendTextNotification(
        long conversationId, 
        string message, 
        bool isError = false,
        CancellationToken cancellationToken = default)
    {
        var dto = new BotTextMessageDto(conversationId, message, isError);
        var requestUri = new Uri(_httpClient.BaseAddress ?? new Uri(_baseUrl), "/api/notify/send/text");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return MoneoResult.Success();
        }

        _logger.LogError(
            "Failed to send notification to conversation {ConversationId}. Response: {Response}", 
            conversationId, 
            response.StatusCode);

        return MoneoResult.Failed($"Failed to send notification. Server reported {response.StatusCode}");
    }
}