using MediatR;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Moneo.Chat.Telegram.Api.StartChatAdapter;

public sealed record StartTelegramRequest(string RequestUrl) : IRequest<MoneoResult>;

internal sealed class StartTelegramRequestHandler : IRequestHandler<StartTelegramRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    
    public StartTelegramRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<MoneoResult> Handle(StartTelegramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var callbackUrl = request.RequestUrl.Replace(
                ChatConstants.Routes.StartAdapter,
                ChatConstants.Routes.ReceiveFromUser);
            
            await _chatAdapter.StartReceivingAsync(callbackUrl, cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}