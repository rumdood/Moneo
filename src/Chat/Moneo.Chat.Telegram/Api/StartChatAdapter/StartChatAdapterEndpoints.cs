using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Moneo.Chat.Telegram.Api.StartChatAdapter;

public static class StartChatAdapterEndpoints
{
    public static void AddStartChatAdapterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost($"/{ChatConstants.Routes.StartAdapterRoute}",
            async (HttpRequestMessage request, ISender sender) =>
            {
                var requestUri = request.RequestUri?.AbsoluteUri;

                if (requestUri is null)
                {
                    return Results.BadRequest();
                }

                var result = await sender.Send(new StartTelegramRequest(requestUri));

                return result.IsSuccess
                    ? Results.Ok()
                    : Results.Problem(detail: result.Message, title: "Internal Server Error");
            });
    }
}
