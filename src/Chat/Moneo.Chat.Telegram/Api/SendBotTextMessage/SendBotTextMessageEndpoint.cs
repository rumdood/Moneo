using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Moneo.Moneo.Chat.Telegram.Api.SendBotTextMessage;

public static class SendBotTextMessageEndpoint
{
    public static RouteHandlerBuilder AddSendBotTextMessageEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost(ChatConstants.Routes.SendTextToUser, async ([FromBody] BotTextMessageDto message, ISender sender) =>
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