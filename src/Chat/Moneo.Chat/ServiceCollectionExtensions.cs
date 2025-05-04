using Microsoft.Extensions.DependencyInjection;
using Moneo.Chat.CommandRegistration;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.Chitchat;
using Moneo.Chat.Workflows.CreateCronSchedule;
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
        var options = new ChatAdapterOptions();
        return services.AddChatAdapter<TChatAdapter>(options);
    }
    
    public static IServiceCollection AddChatAdapter<TChatAdapter>(this IServiceCollection services,
        ChatAdapterOptions options) where TChatAdapter : class, IChatAdapter
    {
        // for now let's automatically add the default commandset
        options.RegisterUserRequestsFromAssemblyContaining<HelpRequest>();
        
        services.AddSingleton<IChatAdapter, TChatAdapter>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(TChatAdapter).Assembly);
            cfg.RegisterServicesFromAssemblies(options.ChatCommandAssemblies.ToArray());
        });
        
        if (!options.IsValid())
        {
            throw new InvalidOperationException("Invalid ChatManagerOptions");
        }

        CommandRegistrar.RegisterCommands(options);
        
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
        services.AddSingleton<IWorkflowWithTaskDraftStateMachineRepository, TaskCreateOrChangeStateMachineRepository>();
        services.AddSingleton<IWorkflowStateMachineRepository<CronWorkflowState>, CronStateMachineRepository>();
        return services;
    }
    
    public static IServiceCollection AddChatManager(this IServiceCollection services)
    {
        services.AddSingleton<IChatManager, ChatManager>();
        return services;
    }
}

public class ChatAdapterOptions : MoneoChatCommandConfiguration
{
    public bool InMemoryStateManagementEnabled { get; private set; } = true;
    public string DefaultTimeZone { get; private set; } = "UTC";
    
    public void UseInMemoryStateManagement() => InMemoryStateManagementEnabled = true;
    public void SetDefaultTimeZone(string timeZone) => DefaultTimeZone = timeZone;

    public bool IsValid() => true;
}