using MediatR;
using Moneo.Chat;

namespace Moneo.TelegramChat.Api.Features.GetStatus;

public sealed record GetStatusResult(ChatAdapterStatus Status);

public sealed record GetStatusRequest : IRequest<IMoneoResult<GetStatusResult>>;

internal sealed class GetStatusRequestHandler : IRequestHandler<GetStatusRequest, IMoneoResult<GetStatusResult>>
{
    private readonly IChatAdapter _chatAdapter;

    public GetStatusRequestHandler(IChatAdapter chatAdapter)
    {
        _chatAdapter = chatAdapter;
    }
    
    public async Task<IMoneoResult<GetStatusResult>> Handle(GetStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var status = await _chatAdapter.GetStatusAsync(cancellationToken);
            return MoneoResult<GetStatusResult>.Success(new GetStatusResult(status));
        }
        catch (Exception e)
        {
            return MoneoResult<GetStatusResult>.Error(e);
        }
    }
}
