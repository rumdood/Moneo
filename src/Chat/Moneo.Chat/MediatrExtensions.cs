using Microsoft.Extensions.DependencyInjection;

namespace Moneo.Chat;

public static class MediatrExtensions
{
    public static MediatRServiceConfiguration RegisterChatAdapter<TAdapter>(this MediatRServiceConfiguration configuration)
        where TAdapter : class, IChatAdapter
    {
        configuration.RegisterServicesFromAssemblies(typeof(TAdapter).Assembly);
        return configuration;
    }
}