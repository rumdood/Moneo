using MediatR;
using Moneo.Chat.Commands;

namespace RadioFreeBot.Features.AddSongToPlaylist;

internal sealed class AddSongRequestHandler : IRequestHandler<AddSongRequest, MoneoCommandResult>
{
    private readonly IAddSongByQueryWorkflowManager _addSongByQueryWorkflowManager;
    private readonly IAddSongByIdWorkflowManager _addSongByIdWorkflowManager;

    public AddSongRequestHandler(IAddSongByQueryWorkflowManager addSongByQueryWorkflowManager, IAddSongByIdWorkflowManager addSongByIdWorkflowManager)
    {
        _addSongByQueryWorkflowManager = addSongByQueryWorkflowManager;
        _addSongByIdWorkflowManager = addSongByIdWorkflowManager;
    }
    
    public Task<MoneoCommandResult> Handle(AddSongRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.SongId))
        {
            return _addSongByIdWorkflowManager.StartWorkflowAsync(request.Context, request.SongId, cancellationToken);
        }
        
        return _addSongByQueryWorkflowManager.StartWorkflowAsync(request.Context, request.PlaylistId, request.SongQuery,
            cancellationToken);
    }
}