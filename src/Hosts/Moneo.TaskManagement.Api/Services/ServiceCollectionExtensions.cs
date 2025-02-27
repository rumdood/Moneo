namespace Moneo.TaskManagement.Api.Services;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationService(
        this IServiceCollection services,
        Action<NotificationOptions> configure)
    {
        var options = new NotificationOptions();
        configure(options);

        if (options.Configuration is null)
        {
            throw new InvalidOperationException("Configuration for notifications is not set");
        }

        services.AddSingleton(options.Configuration);

        services.AddHttpClient<INotificationService, NotificationService>(client =>
        {
            client.BaseAddress = new Uri(options.Host);
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        });

        return services;
    }

    internal class NotificationOptions
    {
        public NotificationConfig? Configuration { get; private set; }

        public string Host
        {
            get
            {
                if (Configuration is null)
                {
                    throw new InvalidOperationException("Configuration is not set");
                }

                return Configuration.BaseUrl;
            }
            set
            {
                if (Configuration == null)
                {
                    Configuration = new NotificationConfig();
                }

                Configuration.BaseUrl = value;
            }
        }

        public string ApiKey
        {
            get
            {
                if (Configuration is null)
                {
                    throw new InvalidOperationException("Configuration is not set");
                }

                return Configuration.ApiKey;
            }
            set
            {
                if (Configuration == null)
                {
                    Configuration = new NotificationConfig();
                }

                Configuration.ApiKey = value;
            }
        }

        public void UseConfiguration(NotificationConfig configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}