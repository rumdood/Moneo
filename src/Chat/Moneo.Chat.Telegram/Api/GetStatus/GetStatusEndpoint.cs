using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Moneo.Chat.Telegram.Api.GetStatus;

public static class GetStatusEndpoint
{
    public static RouteHandlerBuilder AddGetStatusEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet(ChatConstants.Routes.GetStatusRoute, async (ISender sender) =>
        {
            var result = await sender.Send(new GetStatusRequest());
            return result.IsSuccess
                ? Results.Ok(result.Data!)
                : Results.Problem(title: "Internal Server Error", detail: result.Message);
        });
    }
}