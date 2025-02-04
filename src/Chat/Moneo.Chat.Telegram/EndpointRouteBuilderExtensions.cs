using Microsoft.AspNetCore.Routing;
using Moneo.Moneo.Chat.Telegram.Api.GetStatus;
using Moneo.Moneo.Chat.Telegram.Api.ReceiveMessage;
using Moneo.Moneo.Chat.Telegram.Api.SendBotTextMessage;
using Moneo.Moneo.Chat.Telegram.Api.StartChatAdapter;
using Moneo.Moneo.Chat.Telegram.Api.StopChatAdapter;

namespace Moneo.Chat.Telegram;

public static class EndpointRouteBuilderExtensions
{
    public static void AddTelegramChatAdapterEndpoints(this IEndpointRouteBuilder app)
    {
        app.AddGetStatusEndpoint();
        app.AddReceiveMessageEndpoint();
        app.AddSendBotTextMessageEndpoint();
        app.AddStartChatAdapterEndpoint();
        app.AddStopChatAdapterEndpoint();
    }
}