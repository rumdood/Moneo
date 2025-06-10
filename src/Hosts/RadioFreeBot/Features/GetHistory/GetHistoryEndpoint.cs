using MediatR;
using Moneo.Chat.Models;
using Moneo.Web;

namespace RadioFreeBot.Features.GetHistory;

public static class GetHistoryEndpoint
{
    public static RouteHandlerBuilder AddGetHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/history/{playlistId:long}", 
            async (long playlistId, ChatUser user, ISender sender) =>
        {
            var result = await sender.Send(new GetHistoryRequest(user, playlistId));
            return result.GetHttpResult();
        });
    }
}