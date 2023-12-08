using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.BotRequests;

namespace Moneo.Functions.Isolated.Chat
{
    public class TelegramChatFunctions
    {
        private readonly ILogger<TelegramChatFunctions> _logger;
        private readonly IChatAdapter _chatAdapter;

        public TelegramChatFunctions(ILogger<TelegramChatFunctions> logger, IChatAdapter chatAdapter)
        {
            _logger = logger;
            _chatAdapter = chatAdapter;
        }

        [Function(nameof(ConfigureAsync))]
        public async Task<HttpResponseData> ConfigureAsync(
            [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = "configure")] HttpRequestData req)
        {
            var callbackUrl = req.Url.AbsoluteUri.Replace(nameof(ConfigureAsync), nameof(ReceiveUpdateAsync),
                StringComparison.OrdinalIgnoreCase);
            await _chatAdapter.StartReceivingAsync(callbackUrl);
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [Function(nameof(ReceiveUpdateAsync))]
        public async Task<HttpResponseData> ReceiveUpdateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "receive")] HttpRequestData request)
        {
            var message = await request.ReadFromJsonAsync<object>();

            if (message is null)
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            try
            {
                await _chatAdapter.ReceiveUserMessageAsync(message, CancellationToken.None);
                return request.CreateResponse(HttpStatusCode.OK);
            }
            catch (UserMessageFormatException formatException)
            {
                _logger.LogError(formatException, "Received: {@Json}", message);
                return request.CreateResponse(HttpStatusCode.UnprocessableEntity);
            }
        }

        [Function(nameof(SendMessageToUserAsync))]
        public async Task<HttpResponseData> SendMessageToUserAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "send/{messageType:alpha}")]
            HttpRequestData request, string messageType)
        {
            var response = messageType.ToUpperInvariant() switch
            {
                "TEXT" => await ProcessSendMessageRequest<BotTextMessageRequest>(request,
                    (message) => _chatAdapter.SendBotTextMessageAsync(message, CancellationToken.None)),
                "GIF" => await ProcessSendMessageRequest<BotGifMessageRequest>(request,
                    (message) => _chatAdapter.SendBotGifMessageAsync(message, CancellationToken.None)),
                _ => request.CreateResponse(HttpStatusCode.NotFound)
            };

            return response;
        }

        private async Task<HttpResponseData> ProcessSendMessageRequest<TBotMessageType>(HttpRequestData request,
            Func<TBotMessageType, Task> handler)
        {
            var message = await request.ReadFromJsonAsync<TBotMessageType>();

            if (message is null)
            {
                return request.CreateResponse(HttpStatusCode.BadRequest);
            }

            try
            {
                await handler(message);
                return request.CreateResponse(HttpStatusCode.OK);
            }
            catch (UserMessageFormatException formatException)
            {
                _logger.LogError(formatException, "Received bot message request {@Message}", message);
                return request.CreateResponse(HttpStatusCode.UnprocessableEntity);
            }
        }
    }
}