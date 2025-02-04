using Microsoft.Extensions.DependencyInjection;
using Moneo.Chat.ServiceCollectionExtensions;

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
        services.AddWorkflowManagers();
        return services;
    }
}