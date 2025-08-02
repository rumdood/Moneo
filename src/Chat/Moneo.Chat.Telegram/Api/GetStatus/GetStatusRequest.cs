using MediatR;
using Moneo.Chat;
using Moneo.Common;

namespace Moneo.Chat.Telegram.Api.GetStatus;

public sealed record GetStatusResult(ChatAdapterStatus Status);

public sealed record GetStatusRequest : IRequest<MoneoResult<GetStatusResult>>;

internal sealed class GetStatusRequestHandler : IRequestHandler<GetStatusRequest, MoneoResult<GetStatusResult>>
{
    private readonly IChatAdapter _chatAdapter;

    public GetStatusRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<MoneoResult<GetStatusResult>> Handle(GetStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var status = await _chatAdapter.GetStatusAsync(cancellationToken);
            return MoneoResult<GetStatusResult>.Success(new GetStatusResult(status));
        }
        catch (Exception e)
        {
            return MoneoResult<GetStatusResult>.Failed(e);
        }
    }
}
