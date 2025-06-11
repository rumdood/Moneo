using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Chat.Telegram.Api.StartChatAdapter;

public sealed record StartTelegramRequest(string RequestUrl) : IRequest<MoneoResult>;

internal sealed class StartTelegramRequestHandler : IRequestHandler<StartTelegramRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    private readonly ILogger<StartTelegramRequestHandler> _logger;
    
    public StartTelegramRequestHandler(IChatAdapter chatAdapter, ILogger<StartTelegramRequestHandler> logger)
    {
        _chatAdapter = chatAdapter;
        _logger = logger;
    }
    
    public async Task<MoneoResult> Handle(StartTelegramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var callbackUrl = request.RequestUrl.Replace(
                ChatConstants.Routes.StartAdapter,
                ChatConstants.Routes.ReceiveFromUser);
            
            _logger.LogInformation("Attempting to start telegram adapter with callback URL {CallbackUrl}", callbackUrl);
            
            await _chatAdapter.StartReceivingAsync(callbackUrl, cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}