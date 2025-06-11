using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Chat.Telegram.Api.StartChatAdapter;

public static class StartChatAdapterEndpoints
{
    public static RouteHandlerBuilder AddStartChatAdapterEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost(ChatConstants.Routes.StartAdapter,
            async (HttpContext context, ISender sender) =>
            {
                var requestUri = $"https://{context.Request.Host}{context.Request.Path}";

                var result = await sender.Send(new StartTelegramRequest(requestUri));

                return result.IsSuccess
                    ? Results.Ok()
                    : Results.Problem(detail: result.Message, title: "Internal Server Error");
            });
    }
}
