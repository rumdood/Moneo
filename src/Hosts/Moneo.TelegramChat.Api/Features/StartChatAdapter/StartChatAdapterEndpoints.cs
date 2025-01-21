using MediatR;

namespace Moneo.TelegramChat.Api.Features.StartChatAdapter;

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
