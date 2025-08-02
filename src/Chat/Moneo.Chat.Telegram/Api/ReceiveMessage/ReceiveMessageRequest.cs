using System.Text.Json;
using MediatR;
using Moneo.Chat;
using Moneo.Common;
using Telegram.Bot.Types;

namespace Moneo.Chat.Telegram.Api.ReceiveMessage;

public sealed record ReceiveMessageRequest(string JsonMessage) : IRequest<MoneoResult>;

internal sealed class ReceiveMessageRequestHandler : IRequestHandler<ReceiveMessageRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public ReceiveMessageRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<MoneoResult> Handle(ReceiveMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var update = JsonSerializer.Deserialize<Update>(request.JsonMessage, SerializerOptions);

            if (update is null)
            {
                return MoneoResult.BadRequest("Payload was not a valid Update object");
            }
            
            await _chatAdapter.ReceiveUserMessageAsync(update, cancellationToken);
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
