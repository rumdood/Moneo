using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Telegram;

namespace Moneo.Moneo.Chat.Telegram.Api.ReceiveMessage;

public static class ReceiveMessageEndpoint
{
    public static void AddReceiveMessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(ChatConstants.Routes.ReceiveFromUser,
            async (HttpContext context, ISender sender, TelegramChatAdapterOptions options) =>
            {
                // get a logger
                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("ReceiveMessageEndpoint");
                
                if (options is null || string.IsNullOrWhiteSpace(options.CallbackToken))
                {
                    logger.LogWarning("No callback token configured for the Telegram chat adapter");
                    return Results.StatusCode(500);
                }
                
                // get the telegram header
                if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var tokenValues) ||
                    !tokenValues.Any(t => t is not null && t.Equals(options.CallbackToken)))
                {
                    logger.LogWarning("Unauthorized request to receive message endpoint");
                    return Results.Unauthorized();
                }

                using var reader = new StreamReader(context.Request.Body);
                var message = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(message))
                {
                    logger.LogWarning("Received empty message");
                    return Results.BadRequest();
                }
                
                logger.LogInformation("Received message: {Message}", message);
                
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