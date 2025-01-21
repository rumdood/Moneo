using MediatR;
using Moneo.Chat;

namespace Moneo.TelegramChat.Api.Features.SendBotTextMessage;

public sealed record BotTextMessageDto(long ConversationId, string Text, bool IsError) : IBotTextMessage;

public sealed record SendBotTextMessageRequest(BotTextMessageDto Message) : IRequest<IMoneoResult>;

internal sealed class SendMessageRequestHandler
    : IRequestHandler<SendBotTextMessageRequest, IMoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    
    public SendMessageRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }

    public async Task<IMoneoResult> Handle(SendBotTextMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.SendBotTextMessageAsync(request.Message, cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Error(e);
        }
    }
}
