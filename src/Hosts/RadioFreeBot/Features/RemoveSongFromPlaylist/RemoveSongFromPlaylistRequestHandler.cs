using MediatR;
using Moneo.Chat.Commands;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

internal class RemoveSongFromPlaylistRequestHandler : IRequestHandler<RemoveSongFromPlaylistRequest, MoneoCommandResult>
{
    private readonly IRemoveSongFromPlaylistWorkflowManager _workflowManager;
    private readonly ILogger<RemoveSongFromPlaylistRequestHandler> _logger;

    public RemoveSongFromPlaylistRequestHandler(
        IRemoveSongFromPlaylistWorkflowManager workflowManager,
        ILogger<RemoveSongFromPlaylistRequestHandler> logger)
    {
        _workflowManager = workflowManager;
        _logger = logger;
    }
    
    public async Task<MoneoCommandResult> Handle(RemoveSongFromPlaylistRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling RemoveSongFromPlaylistRequest for song ID: {SongName}", request.SongName);
        
        if (string.IsNullOrEmpty(request.SongName))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Song Name cannot be empty."
            };
        }
        
        try
        {
            var result = await _workflowManager.StartWorkflowAsync(request.Context, request.SongName, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing RemoveSongFromPlaylistRequest for song ID: {SongName}", request.SongName);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"An error occurred while trying to remove the song: {ex.Message}"
            };
        }
    }
}