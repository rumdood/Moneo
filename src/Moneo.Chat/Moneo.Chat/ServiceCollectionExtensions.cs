using Microsoft.Extensions.DependencyInjection;
using Moneo.Core;

namespace Moneo.Chat.ServiceCollectionExtensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatAdapter(this IServiceCollection services, IBotClientConfiguration botConfig)
    {
        // Use reflection to locate the appropriate class
        var chatAdapterType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name.Equals(botConfig.ChatAdapter, StringComparison.OrdinalIgnoreCase));
        
        if (chatAdapterType is null || typeof(IChatAdapter).IsAssignableFrom(chatAdapterType) == false)
        {
            throw new InvalidOperationException(
                $"Chat adapter '{botConfig.ChatAdapter}' not found or does not implement IChatAdapter.");
        }
        
        services.AddSingleton(typeof(IChatAdapter), chatAdapterType);
        
        return services;
    }
    
    public static IServiceCollection AddChatAdapter<TChatAdapter>(this IServiceCollection services)
        where TChatAdapter : class, IChatAdapter
    {
        services.AddSingleton<IChatAdapter, TChatAdapter>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(CompleteTaskRequest).Assembly, typeof(TChatAdapter).Assembly);
        });
        return services;
    }
    
    public static IServiceCollection AddMediatr(this IServiceCollection services, IBotClientConfiguration botConfig)
    {
        services.AddMediatR(cfg =>
        {
            // check to see if we already have the IChatAdapter registered
            var chatAdapterType =
                services.FirstOrDefault(s => s.ServiceType == typeof(IChatAdapter))?.ImplementationType ??
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name.Equals(botConfig.ChatAdapter, StringComparison.OrdinalIgnoreCase));
            
            var adapterAssembly = chatAdapterType?.Assembly ?? throw new InvalidOperationException(
                $"Chat adapter '{botConfig.ChatAdapter}' not found.");
            
            cfg.RegisterServicesFromAssemblies(typeof(CompleteTaskRequest).Assembly, adapterAssembly);
        });
        
        return services;
    }
}