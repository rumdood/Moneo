using Microsoft.Extensions.DependencyInjection;
using Moneo.Chat.ServiceCollectionExtensions;
using Telegram.Bot;

namespace Moneo.Chat.Telegram;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramChatAdapter(this IServiceCollection services, Action<TelegramChatAdapterOptions> options)
    {
        var adapterOptions = new TelegramChatAdapterOptions();
        options.Invoke(adapterOptions);
        
        services.AddTelegramChatAdapter(adapterOptions);
        return services;
    }
    
    public static IServiceCollection AddTelegramChatAdapter(this IServiceCollection services, TelegramChatAdapterOptions options)
    {
        services.AddSingleton(options);
        services.AddChatAdapter<TelegramChatAdapter>(options);
        services.AddChatManager();
        
        // Register named HttpClient to get benefits of IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        // More read:
        //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
        //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddHttpClient("tgclient")
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(options.BotToken, httpClient));

        if (options.HostedServiceFlag)
        {
            services.AddHostedService<TelegramChatBackgroundService>();
        }
        
        return services;
    }
}