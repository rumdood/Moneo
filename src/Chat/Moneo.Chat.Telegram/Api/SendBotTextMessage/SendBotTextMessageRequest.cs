using MediatR;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Chat.Telegram.Api.SendBotTextMessage;

public sealed record BotTextMessageDto(long ConversationId, string Text, bool IsError) : IBotTextMessage;

public sealed record SendBotTextMessageRequest(BotTextMessageDto Message) : IRequest<MoneoResult>;

internal sealed class SendMessageRequestHandler
    : IRequestHandler<SendBotTextMessageRequest, MoneoResult>
{
    private readonly IChatAdapter _chatAdapter;
    
    public SendMessageRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }

    public async Task<MoneoResult> Handle(SendBotTextMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatAdapter.SendBotTextMessageAsync(request.Message, cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}
