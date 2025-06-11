using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Moneo.Chat.Telegram.Api.GetStatus;
using Moneo.Chat.Telegram.Api.ReceiveMessage;
using Moneo.Chat.Telegram.Api.SendBotTextMessage;
using Moneo.Chat.Telegram.Api.StartChatAdapter;
using Moneo.Chat.Telegram.Api.StopChatAdapter;

namespace Moneo.Chat.Telegram;

public static class EndpointRouteBuilderExtensions
{
    public static void AddTelegramChatAdapterEndpoints(this IEndpointRouteBuilder app, Action<TelegramEndpointOptions>? configureOptions = null)
    {
        var options = new TelegramEndpointOptions();
        configureOptions?.Invoke(options);
        app.AddTelegramChatAdapterEndpoints(options);
    }
    
    private static void AddTelegramChatAdapterEndpoints(this IEndpointRouteBuilder app, TelegramEndpointOptions? options = null)
    {
        var statusEndpoint = app.AddGetStatusEndpoint();
        var sendBotTextMessageEndpoint = app.AddSendBotTextMessageEndpoint();
        var startAdapterEndpoint = app.AddStartChatAdapterEndpoint();
        var stopAdapterEndpoint = app.AddStopChatAdapterEndpoint();
        
        // this endpoint cannot use normal authorization, as it is called by Telegram
        app.AddReceiveMessageEndpoint();

        if (string.IsNullOrEmpty(options?.AuthorizationPolicy))
        {
            return;
        }
        
        // add the authorization policy to all other endpoints
        statusEndpoint.RequireAuthorization();
        sendBotTextMessageEndpoint.RequireAuthorization();
        startAdapterEndpoint.RequireAuthorization();
        stopAdapterEndpoint.RequireAuthorization();
    }
}

public class TelegramEndpointOptions
{
    public string? AuthorizationPolicy { get; private set; }
    
    public void RequireAuthorization(string policy)
    {
        AuthorizationPolicy = policy;
    }
}