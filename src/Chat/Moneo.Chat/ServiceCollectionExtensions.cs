using Microsoft.Extensions.DependencyInjection;
using Moneo.Common;

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
    
    public static IServiceCollection AddChatAdapter<TChatAdapter>(this IServiceCollection services,
        ChatAdapterOptions options) where TChatAdapter : class, IChatAdapter
    {
        services.AddChatAdapter<TChatAdapter>();
        if (!options.IsValid())
        {
            throw new InvalidOperationException("Invalid ChatManagerOptions");
        }
        
        if (options.InMemoryStateManagementEnabled)
        {
            services.AddInMemoryChatStateManagement();
        }
        return services;
    }

    public static IServiceCollection AddChatAdapter<TChatAdapter>(this IServiceCollection services,
        Action<ChatAdapterOptions> options) where TChatAdapter : class, IChatAdapter
    {
        services.AddChatAdapter<TChatAdapter>();
        var chatAdapterOptions = new ChatAdapterOptions();
        options.Invoke(chatAdapterOptions);

        services.AddChatAdapter<TChatAdapter>(chatAdapterOptions);
        return services;
    }
    
    public static IServiceCollection AddInMemoryChatStateManagement(this IServiceCollection services)
    {
        services.AddSingleton<IChatStateRepository, InMemoryChatStateRepository>();
        return services;
    }
    
    public static IServiceCollection AddChatManager(this IServiceCollection services)
    {
        services.AddSingleton<IChatManager, ChatManager>();
        return services;
    }
}

public class ChatAdapterOptions
{
    public bool InMemoryStateManagementEnabled { get; private set; } = true;
    
    public void UseInMemoryStateManagement() => InMemoryStateManagementEnabled = true;

    public bool IsValid() => true;
}