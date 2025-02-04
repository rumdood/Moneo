using MediatR;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Moneo.Chat.Telegram.Api.ReceiveMessage;

public sealed record ReceiveMessageRequest(string JsonMessage) : IRequest<MoneoResult>;

internal sealed class ReceiveMessageRequestHandler : IRequestHandler<ReceiveMessageRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    
    public ReceiveMessageRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<MoneoResult> Handle(ReceiveMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.ReceiveUserMessageAsJsonAsync(request.JsonMessage, cancellationToken);
            return MoneoResult.Success();
        }
        catch (UserMessageFormatException ufe)
        {
            return MoneoResult.Failed(ChatConstants.ErrorMessages.UserMessageFormatInvalid, ufe);
        }
        catch (Exception ex)
        {
            return MoneoResult.Failed(ex);
        }
    }
}
