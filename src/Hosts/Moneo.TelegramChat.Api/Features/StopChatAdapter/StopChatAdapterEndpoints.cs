using MediatR;
using Moneo.TelegramChat.Api.Features.StartChatAdapter;

namespace Moneo.TelegramChat.Api.Features.StopChatAdapter;

public static class StopChatAdapterEndpoints
{
    public static void AddStopChatAdapterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete($"/{ChatConstants.Routes.StopAdapterRoute}", async (ISender sender) =>
        {
            var result = await sender.Send(new StopTelegramRequest());

            return result.IsSuccess
                ? Results.Ok()
                : Results.Problem(detail: result.Message, title: "Internal Server Error");
        });
    }
}