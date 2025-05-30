using MediatR;
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
    private readonly RadioFreeDbContext _context;
    
    public AddSongByIdWorkflowManager(IMediator mediator, IYouTubeMusicProxyClient youTubeMusicProxyClient, RadioFreeDbContext dbContext) : base(mediator)
    {
        _youTubeMusicProxyClient = youTubeMusicProxyClient;
        _context = dbContext;
    }

    private static string GetResponseTextFromErrorMessage(string errorMessage)
    {
        if (errorMessage.Equals("Song already in playst", StringComparison.InvariantCultureIgnoreCase))
        {
            return "That song is already in the playlist.";
        }

        return $"I couldn't add that song to the playlist. The reason was something to do with '{errorMessage}'";
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

        var playlist = _context.Playlists.FirstOrDefault(pl => pl.ConversationId == cmdContext.ConversationId);
        
        if (playlist == null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "No playlist found for this conversation."
            };
        }
        
        // Attempt to add the song to the playlist using the YouTube Music proxy client
        var result = await _youTubeMusicProxyClient.AddSongToPlaylistAsync(playlist.ExternalId, songId, cancellationToken);
        var user = cmdContext.User is not null ? "@" + cmdContext.User.Username : "you";

        if (result.IsSuccess)
        {
            // TODO: Change the API this calls so that it returns the song info directly
            var songInfo = await GetSongInfoFromPlaylist(playlist.ExternalId, songId, cancellationToken);
            if (songInfo is not null)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.WorkflowCompleted,
                    UserMessageText = $"I added \"{songInfo.Title}\" to the playlist for {user}! 🎶",
                };
            }

            return new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    $"I added the song to the playlist for {user}, but I couldn't retrieve the song details. Please check the playlist manually."
            };
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = GetResponseTextFromErrorMessage(result.Message)
        };

        async Task<SongItem?> GetSongInfoFromPlaylist(string playlistId, string songId,
            CancellationToken cancellationToken = default)
        {
            var getSongResult = await _youTubeMusicProxyClient.GetSongFromPlaylistAsync(playlistId, songId, cancellationToken);
            return getSongResult.Data;
        }
    }
}