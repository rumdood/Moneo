using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Common;
using RadioFreeBot.Models;
using RadioFreeBot.ResourceAccess;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.Features.AddSongToPlaylist;

public interface IAddSongByIdWorkflowManager
{
    // Define methods for managing the workflow of adding a song by ID
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string songId, CancellationToken cancellationToken = default);
}

[MoneoWorkflow]
public class AddSongByIdWorkflowManager : WorkflowManagerBase, IAddSongByIdWorkflowManager
{
    private readonly IYouTubeMusicProxyClient _youTubeMusicProxyClient;
    private readonly RadioFreeDbContext _dbContext;
    private readonly ILogger<AddSongByIdWorkflowManager> _logger;
    private readonly TimeProvider _timeProvider;
    private const string YouTubeSongUrl = "https://music.youtube.com/watch?v=";
    
    public AddSongByIdWorkflowManager(
        IMediator mediator, 
        IYouTubeMusicProxyClient youTubeMusicProxyClient, 
        RadioFreeDbContext dbDbContext, 
        ILogger<AddSongByIdWorkflowManager> logger,
        TimeProvider timeProvider) : base(mediator)
    {
        _youTubeMusicProxyClient = youTubeMusicProxyClient;
        _dbContext = dbDbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private static string GetResponseTextFromErrorMessage(string errorMessage)
    {
        if (errorMessage.Equals("Song already in playst", StringComparison.InvariantCultureIgnoreCase))
        {
            return "That song is already in the playlist.";
        }

        return $"I couldn't add that song to the playlist. The reason was something to do with '{errorMessage}'";
    }

    /// <summary>
    /// Updates the database with the song information after adding it to the playlist.
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="playlistExternalId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<MoneoResult> UpdateDatabaseAsync(SongItem song, long playlistId, CommandContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.User is null)
        {
            return MoneoResult.NotFound("User not found in the context.");
        }
        
        try
        {
            // Check if the song already exists in the database
            var existingSong =
                await _dbContext.Songs.Where(s => s.ExternalId == song.Id).FirstOrDefaultAsync(cancellationToken);
        
            if (existingSong == null)
            {
                // If the song does not exist, create a new entry
                existingSong = new Song(song.Title, song.Id, YouTubeSongUrl + song.Id);
                _dbContext.Songs.Add(existingSong);
                _logger.LogDebug("Adding new song {SongId} to the database for user {UserId}", song.Id, context.User?.Id ?? 0);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            
            var existingUser =
                await _dbContext.Users.Where(u => u.TelegramId == context.User!.Id).FirstOrDefaultAsync(cancellationToken);
            
            if (existingUser == null)
            {
                // If the user does not exist, create a new entry
                existingUser = new User(context.User!.Username ?? context.User!.FirstName, context.User!.Id);
                _dbContext.Users.Add(existingUser);
                _logger.LogDebug("Adding new user {UserId} to the database", context.User!.Id);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        
            // create a new PlaylistSong entry
            var playlistSong = new PlaylistSong(playlistId, existingSong.Id, _timeProvider.GetUtcNow().UtcDateTime, existingUser.Id);
            _dbContext.PlaylistSongs.Add(playlistSong);
            _logger.LogDebug("Adding song {SongId} to playlist {PlaylistId} for user {UserId}", song.Id, playlistId, existingUser.TelegramId);
            await _dbContext.SaveChangesAsync(cancellationToken);
        
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding song {SongId} to the Playlist {PlaylistId} in the database", song.Id, playlistId);
            return MoneoResult.Failed($"Failed to add song {song.Id} to the playlist {playlistId} in the database: {e.Message}");
        }
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string songId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(songId))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Song ID cannot be empty."
            };
        }

        if (cmdContext.User is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I am unclear as to what just happened but I'm ignoring it."
            };
        }
        
        var playlist = _dbContext.Playlists.FirstOrDefault(pl => pl.ConversationId == cmdContext.ConversationId);
        
        if (playlist == null)
        {
            _logger.LogWarning("No playlist found for conversation {ConversationId}", cmdContext.ConversationId);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "No playlist found for this conversation. Contact @rumdood"
            };
        }
        
        var user = await _dbContext.GetOrCreateUserPlaylistHistoryForTelegramAsync(
            cmdContext.User.Id,
            cmdContext.User.Username ?? cmdContext.User.FirstName,
            true,
            playlist.Id,
            cancellationToken);
        
        var username = "@" + user.Name;
        
        if (user.History.Count(h => _timeProvider.GetUtcNow().UtcDateTime.Subtract(h.OccurredOn) < TimeSpan.FromHours(24)) >= 2)
        {
            _logger.LogWarning("User {UserId} has reached the daily limit of adding songs to the playlist", user.TelegramId);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"Sorry, @{user.Name}, you've already added a song today. Please try again tomorrow."
            };
        }
        
        // Attempt to add the song to the playlist using the YouTube Music proxy client
        var result = await _youTubeMusicProxyClient.AddSongToPlaylistAsync(playlist.ExternalId, songId, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogDebug("Successfully added song {SongId} to playlist {PlaylistId} for user {User}", songId, playlist.ExternalId, username);
            
            // TODO: Change the API this calls so that it returns the song info directly
            var songInfo = await GetSongInfoFromPlaylist(playlist.ExternalId);
            if (songInfo is not null)
            {
                // Update the database with the song information
                _ = await UpdateDatabaseAsync(songInfo, playlist.Id, cmdContext, cancellationToken);
                
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.WorkflowCompleted,
                    UserMessageText = $"I added \"{songInfo.Title}\" to the playlist for {username}! 🎶",
                };
            }

            return new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    $"I added the song to the playlist for {username}, but I couldn't retrieve the song details. Please check the playlist manually."
            };
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = GetResponseTextFromErrorMessage(result.Message)
        };

        async Task<SongItem?> GetSongInfoFromPlaylist(string playlistExternalId)
        {
            var getSongResult = await _youTubeMusicProxyClient.GetSongFromPlaylistAsync(playlistExternalId, songId, cancellationToken);
            _logger.LogDebug(
                "Attempted to get song info for {SongId} from playlist {PlaylistId}, result: {@GetSongResult}",
                songId, 
                playlistExternalId, 
                getSongResult);
            
            return getSongResult.Data;
        }
    }
}