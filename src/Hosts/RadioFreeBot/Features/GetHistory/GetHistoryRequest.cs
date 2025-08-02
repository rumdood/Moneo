using MediatR;
using Moneo.Chat.Models;
using Moneo.Common;
using RadioFreeBot.Features.AddSongToPlaylist;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Features.GetHistory;

public record GetHistoryRequest(ChatUser User, long PlaylistId) : IRequest<MoneoResult<UserWithHistory>>
{
}

internal class GetHistoryRequestHandler : IRequestHandler<GetHistoryRequest, MoneoResult<UserWithHistory>>
{
    private readonly RadioFreeDbContext _context;

    public GetHistoryRequestHandler(RadioFreeDbContext context)
    {
        _context = context;
    }

    public async Task<MoneoResult<UserWithHistory>> Handle(GetHistoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context
                .GetUserPlaylistHistoryForTelegramAsync(
                    request.PlaylistId,
                    request.User.Id, 
                    cancellationToken: cancellationToken);

            if (user == null)
            {
                return MoneoResult<UserWithHistory>.NotFound("User not found or has no history.");
            }

            return MoneoResult<UserWithHistory>.Success(user);
        }
        catch (Exception e)
        {
            return MoneoResult<UserWithHistory>.Failed(e.Message, e);
        }
    }
}