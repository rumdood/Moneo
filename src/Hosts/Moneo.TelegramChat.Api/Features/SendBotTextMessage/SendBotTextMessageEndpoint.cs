using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Moneo.TelegramChat.Api.Features.SendBotTextMessage;

public static class SendBotTextMessageEndpoint
{
    public static void AddSendBotTextmessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost($"/{ChatConstants.Routes.SendToUser}/text", async ([FromBody] BotTextMessageDto message, ISender sender) =>
        {
            var result = await sender.Send(new SendBotTextMessageRequest(message));
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