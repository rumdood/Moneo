using Microsoft.EntityFrameworkCore;
using RadioFreeBot.Configuration;
using RadioFreeBot.Features.GetHistory;
using RadioFreeBot.ResourceAccess;
using RadioFreeBot.YouTube;

namespace RadioFreeBot;

public static class ServiceCollectionExtensions
{
    public static bool IsRadioFreeBotConfigurationValid(this IServiceCollection services,
        RadioFreeBotConfiguration? botConfiguration)
    {
        if (botConfiguration == null)
        {
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(botConfiguration.YouTubeMusicProxy.YouTubeMusicProxyUrl))
        {
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(botConfiguration.YouTubeVideos.ApiKey))
        {
            return false;
        }

        return true;
    }
    
    public static IServiceCollection AddYouTubeMusicProxyClient(this IServiceCollection services,
        Action<YouTubeMusicProxyOptions> configure)
    {
        var options = new YouTubeMusicProxyOptions();
        configure(options);
        return services.AddYouTubeMusicProxyClient(options);
    }
    
    private static IServiceCollection AddYouTubeMusicProxyClient(this IServiceCollection services,
        YouTubeMusicProxyOptions options)
    {
        services.AddHttpClient<IYouTubeMusicProxyClient, YouTubeMusicProxyClient>(client =>
        {
            client.BaseAddress = new Uri(options.YouTubeMusicProxyUrl);
        });
        return services;
    }
    
    public static IServiceCollection AddPlaylistManagement(this IServiceCollection services, RadioFreeBotOptions options)
    {
        services.AddSingleton(options.TimeProvider);
        services.AddYouTubeMusicProxyClient(options.YouTubeMusicProxyOptions);
        services.AddSingleton<IRadioFreeYouTubeService, RadioFreeYouTubeService>();
        services.AddScoped<AuditingInterceptor>();
        services.AddDbContext<RadioFreeDbContext>((sp, opt) =>
        {
            opt.UseSqlite(options.ConnectionString);
            opt.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });
        return services;
    }

    public static IServiceCollection AddPlaylistManagement(this IServiceCollection services,
        Action<RadioFreeBotOptions> options)
    {
        var rfbOptions = new RadioFreeBotOptions();
        options.Invoke(rfbOptions);
        return services.AddPlaylistManagement(rfbOptions);
    }
}

public class RadioFreeBotOptions
{
    private const string DefaultConnectionString = "Data Source=RadioFreeBot.sqlite";

    public YouTubeMusicProxyOptions YouTubeMusicProxyOptions { get; set; } = new();
    
    public string ConnectionString { get; private set; } = DefaultConnectionString;
    
    public TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    public void ConfigureTimeProvider(Action<TimeProvider> configure)
    {
        configure(TimeProvider);
    }
    
    public void ConfigureYouTubeProxy(Action<YouTubeMusicProxyOptions> configure)
    {
        configure(YouTubeMusicProxyOptions);
    }
    
    public void UseSqliteDatabase(string connectionString = DefaultConnectionString)
    {
        ConnectionString = connectionString;
    }
}

public static class AppBuilderExtensions
{
    public static void AddRadioFreeBotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/about", () => "Radio Free Bot API");
        var getHistoryEndpoint = app.AddGetHistoryEndpoint();
    }
}
