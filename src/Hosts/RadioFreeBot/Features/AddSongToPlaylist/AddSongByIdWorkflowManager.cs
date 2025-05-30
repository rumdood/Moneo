using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using RadioFreeBot.ResourceAccess;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.Features.AddSongToPlaylist;

public interface IAddSongByIdWorkflowManager
{
    // Define methods for managing the workflow of adding a song by ID
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, long forUserId, string songId, CancellationToken cancellationToken = default);
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

    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId, long forUserId, string songId, CancellationToken cancellationToken = default)
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

        var playlist = _context.Playlists.FirstOrDefault(pl => pl.ConversationId == chatId);
        
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

        return result.IsSuccess
            ? new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"Song with ID {songId} has been successfully added to the playlist."
            }
            : new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = GetResponseTextFromErrorMessage(result.Message)
            };
    }
}