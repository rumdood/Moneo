using MediatR;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Chat.Telegram.Api.StopChatAdapter;

public sealed record StopTelegramRequest : IRequest<MoneoResult>;

internal sealed class StopTelegramRequestHandler : IRequestHandler<StopTelegramRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;

    public StopTelegramRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<MoneoResult> Handle(StopTelegramRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.StopReceivingAsync(cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}
