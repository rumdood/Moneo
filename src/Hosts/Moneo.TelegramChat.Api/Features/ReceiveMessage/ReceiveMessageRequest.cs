using MediatR;
using Moneo.Chat;

namespace Moneo.TelegramChat.Api.Features.ReceiveMessage;

public sealed record ReceiveMessageRequest(string JsonMessage) : IRequest<IMoneoResult>;

internal sealed class ReceiveMessageRequestHandler(IChatAdapter chatAdapter) : IRequestHandler<ReceiveMessageRequest, IMoneoResult>
{
    private readonly IChatAdapter _chatAdapter = chatAdapter;
    
    public async Task<IMoneoResult> Handle(ReceiveMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.ReceiveUserMessageAsJsonAsync(request.JsonMessage, cancellationToken);
            return MoneoResult.Success();
        }
        catch (UserMessageFormatException ufe)
        {
            return MoneoResult.Error(ChatConstants.ErrorMessages.UserMessageFormatInvalid, ufe);
        }
        catch (Exception ex)
        {
            return MoneoResult.Error(ex);
        }
    }
}
