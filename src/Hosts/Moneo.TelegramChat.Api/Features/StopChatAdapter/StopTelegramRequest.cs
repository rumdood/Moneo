using MediatR;
using Moneo.Chat;

namespace Moneo.TelegramChat.Api.Features.StopChatAdapter;

public sealed record StopTelegramRequest : IRequest<IMoneoResult>;

internal sealed class StopTelegramRequestHandler : IRequestHandler<StopTelegramRequest, IMoneoResult>
{
    private readonly IChatAdapter _chatAdapter;

    public StopTelegramRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<IMoneoResult> Handle(StopTelegramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.StopReceivingAsync(cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Error(e);
        }
    }
}
