using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.BotRequests;
using Moneo.Core;
using Telegram.Bot.Types;

namespace Moneo.Functions.Isolated.Chat
{
    public class TelegramChatFunctions
    {
        private readonly ILogger<TelegramChatFunctions> _logger;
        private readonly IChatAdapter _chatAdapter;
        private readonly IBotClientConfiguration _botClientConfiguration;

        public TelegramChatFunctions(ILogger<TelegramChatFunctions> logger, IBotClientConfiguration botClientConfiguration, IChatAdapter chatAdapter)
        {
            _logger = logger;
            _botClientConfiguration = botClientConfiguration;
            _chatAdapter = chatAdapter;
        }

        [Function(nameof(Configure))]
        public async Task<HttpResponseData> Configure(
            [HttpTrigger(AuthorizationLevel.Admin, "post", "delete", Route = "configure")] HttpRequestData req)
        {
            var callbackUrl = req.Url.AbsoluteUri.Replace(nameof(Configure), "receive", StringComparison.OrdinalIgnoreCase);
            var response = req.Method.ToLowerInvariant() switch
            {
                "post" => await GetResponseErrorWrapper(req, () => _chatAdapter.StartReceivingAsync(callbackUrl), HttpStatusCode.OK, HttpStatusCode.InternalServerError),
                "delete" => await GetResponseErrorWrapper(req, () => _chatAdapter.StopReceivingAsync(), HttpStatusCode.OK, HttpStatusCode.InternalServerError),
                _ => req.CreateResponse(HttpStatusCode.MethodNotAllowed)
            };

            return response;
        }


        [Function(nameof(ReceiveMessageFromUser))]
        public async Task<HttpResponseData> ReceiveMessageFromUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "receive")] HttpRequestData request)
        {
            // get the telegram header
            if (!request.Headers.TryGetValues("X-Telegram-Bot-Api-Secret-Token", out var tokenValues) || !tokenValues.Any(t => t.Equals(_botClientConfiguration.CallbackToken)))
            {
                return request.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var message = await request.ReadFromJsonAsync<Update>();

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
            var response = messageType.ToLowerInvariant() switch
            {
                "text" => await ProcessSendMessageRequest<BotTextMessageRequest>(request,
                    (message) => _chatAdapter.SendBotTextMessageAsync(message, CancellationToken.None)),
                "gif" => await ProcessSendMessageRequest<BotGifMessageRequest>(request,
                    (message) => _chatAdapter.SendBotGifMessageAsync(message, CancellationToken.None)),
                _ => request.CreateResponse(HttpStatusCode.NotFound)
            };

            return response;
        }

        [Function(nameof(Status))]
        public async Task<HttpResponseData> Status(
                       [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "status")] HttpRequestData req)
        {
            var status = await _chatAdapter.GetStatusAsync(CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(status);

            return response;
        }

        private async Task<HttpResponseData> GetResponseErrorWrapper(HttpRequestData req, Func<Task> f, HttpStatusCode successCode, HttpStatusCode failureCode)
        {
            try
            {
                await f();
                return req.CreateResponse(successCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing {@Method} request at {@Url}", req.Method, req.Url);

                if (_botClientConfiguration.IsDetailedErrorsEnabled)
                {
                    var errorResponse = req.CreateResponse(failureCode);
                    await errorResponse.WriteAsJsonAsync(ex);
                    return errorResponse;
                }
                else
                {
                    return req.CreateResponse(failureCode);
                }
            }
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