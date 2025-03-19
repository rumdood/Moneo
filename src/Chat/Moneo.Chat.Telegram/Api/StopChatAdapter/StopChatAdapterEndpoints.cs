using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Moneo.Chat.Telegram.Api.StopChatAdapter;

public static class StopChatAdapterEndpoints
{
    public static RouteHandlerBuilder AddStopChatAdapterEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(ChatConstants.Routes.StopAdapter, async (ISender sender) =>
        {
            var result = await sender.Send(new StopTelegramRequest());

            return result.IsSuccess
                ? Results.Ok()
                : Results.Problem(detail: result.Message, title: "Internal Server Error");
        });
    }
}