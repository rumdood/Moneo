using Microsoft.EntityFrameworkCore;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYouTubeMusicProxyClient(this IServiceCollection services,
        Action<YouTubeProxyOptions> configure)
    {
        var options = new YouTubeProxyOptions();
        configure(options);
        return services.AddYouTubeMusicProxyClient(options);
    }
    
    private static IServiceCollection AddYouTubeMusicProxyClient(this IServiceCollection services,
        YouTubeProxyOptions options)
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
        services.AddYouTubeMusicProxyClient(options.YouTubeProxyOptions);
        services.AddDbContext<RadioFreeDbContext>((_, opt) =>
        {
            opt.UseSqlite(options.ConnectionString);
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

    public YouTubeProxyOptions YouTubeProxyOptions { get; set; } = new();
    
    public string ConnectionString { get; private set; } = DefaultConnectionString;
    
    public TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    public void ConfigureTimeProvider(Action<TimeProvider> configure)
    {
        configure(TimeProvider);
    }
    
    public void ConfigureYouTubeProxy(Action<YouTubeProxyOptions> configure)
    {
        configure(YouTubeProxyOptions);
    }
    
    public void UseSqliteDatabase(string connectionString = DefaultConnectionString)
    {
        ConnectionString = connectionString;
    }
}