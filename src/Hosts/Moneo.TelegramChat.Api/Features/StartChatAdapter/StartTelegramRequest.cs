using MediatR;
using Moneo.Chat;

namespace Moneo.TelegramChat.Api.Features.StartChatAdapter;

public sealed record StartTelegramRequest(string RequestUrl) : IRequest<IMoneoResult>;

internal sealed class StartTelegramRequestHandler(IChatAdapter chatAdapter) : IRequestHandler<StartTelegramRequest, IMoneoResult>
{
    private readonly IChatAdapter _chatAdapter = chatAdapter;
    
    public async Task<IMoneoResult> Handle(StartTelegramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var callbackUrl = request.RequestUrl.Replace(
                ChatConstants.Routes.StartAdapterRoute,
                ChatConstants.Routes.ReceiveFromUser);
            
            await _chatAdapter.StartReceivingAsync(callbackUrl, cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Error(e);
        }
    }
}