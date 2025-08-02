using MediatR;
using Microsoft.EntityFrameworkCore;
using RadioFreeBot.Configuration;
using RadioFreeBot.Features.LoadPlaylist;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Events;

public sealed record ApplicationStartedEvent(DateTime OccurredOn) : INotification;

internal sealed class ApplicationStartedEventHandler : INotificationHandler<ApplicationStartedEvent>
{
    private readonly ILogger<ApplicationStartedEventHandler> _logger;
    private readonly ISender _sender;
    private readonly RadioFreeDbContext _dbContext;
    private readonly RadioFreeBotConfiguration _config;

    public ApplicationStartedEventHandler(
        ILogger<ApplicationStartedEventHandler> logger,
        ISender sender,
        RadioFreeBotConfiguration configuration,
        RadioFreeDbContext dbContext)
    {
        _logger = logger;
        _sender = sender;
        _dbContext = dbContext;
        _config = configuration;
    }

    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application started");
    
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        if (canConnect)
        {
            _logger.LogInformation("Database exists and is reachable.");

            if (!_config.LoadPlaylistsFromYouTubeOnStartup)
            {
                return;
            }
            
            // get all of the unique playlist external IDs from the database
            var uniquePlaylistIds = await _dbContext.Playlists
                .AsNoTracking()
                .Select(p => p.ExternalId)
                .Distinct()
                .ToListAsync(cancellationToken);
            
            foreach (var playlistId in uniquePlaylistIds)
            {
                _logger.LogInformation("Loading playlist with ID: {PlaylistId}", playlistId);
                
                // Load each playlist using the LoadPlaylistRequest
                var loadPlaylistRequest = new LoadPlaylistRequest(playlistId);
                var result = await _sender.Send(loadPlaylistRequest, cancellationToken);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully loaded playlist with ID: {PlaylistId}", playlistId);
                }
                else
                {
                    _logger.LogError("Failed to load playlist with ID: {PlaylistId}. Error: {ErrorMessage}",
                        playlistId, result.Message);
                }
            }
        }
        else
        {
            _logger.LogError("Database does not exist or is not reachable.");
            // exit the application in an error state
            throw new InvalidOperationException("Database connection failed. Please check your configuration.");
        }
    }
}
