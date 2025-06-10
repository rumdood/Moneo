using MediatR;
using Moneo.Chat;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

public record RemoveSongFromPlaylistWorkflowStartedEvent(CommandContext Context) : IRequest;
public record RemoveSongFromPlaylistWorkflowCompletedEvent(CommandContext Context) : IRequest;

internal class RemovePlaylistSongWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<RemoveSongFromPlaylistWorkflowStartedEvent>
{
    public async Task Handle(RemoveSongFromPlaylistWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(
            request.Context.ConversationId,
            request.Context.User?.Id ?? 0,
            RadioFreeChatStates.RemoveSongFromPlaylist);
    }
}

internal class RemovePlaylistSongWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<RemoveSongFromPlaylistWorkflowCompletedEvent>
{
    public async Task Handle(RemoveSongFromPlaylistWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.Context.ConversationId, request.Context.User?.Id ?? 0);
    }
}