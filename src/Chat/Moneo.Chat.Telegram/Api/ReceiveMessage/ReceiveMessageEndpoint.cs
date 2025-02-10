using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Moneo.Chat.Telegram.Api.ReceiveMessage;

public static class ReceiveMessageEndpoint
{
    public static void AddReceiveMessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(ChatConstants.Routes.ReceiveFromUser,
            async (HttpRequestMessage requestMessage, ISender sender) =>
            {
                // get the telegram header
                if (!requestMessage.Headers.TryGetValues("X-Telegram-Bot-Api-Secret-Token", out var tokenValues) ||
                    !tokenValues.Any(t =>
                        t.Equals("_botClientConfiguration.CallbackToken"))) // TODO: replace with actual token
                {
                    return Results.Unauthorized();
                }

                if (requestMessage.Content is null)
                {
                    return Results.BadRequest();
                }

                var message = await requestMessage.Content.ReadAsStringAsync();
                var result = await sender.Send(new ReceiveMessageRequest(message));
                return result.IsSuccess
                    ? Results.Ok()
                    : result.Message switch
                    {
                        ChatConstants.ErrorMessages.UserMessageFormatInvalid => Results.UnprocessableEntity(),
                        _ => Results.Problem(detail: result.Exception?.Message, title: "Internal Server Error")
                    };
            });
    }
}