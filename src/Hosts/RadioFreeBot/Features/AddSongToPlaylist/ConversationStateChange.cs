using MediatR;
using Moneo.Chat;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

public record AddSongToPlaylistWorkflowStartedEvent(long ChatId, long UserId) : IRequest;
public record AddSongToPlaylistWorkflowCompletedEvent(long ChatId, long UserId) : IRequest;

internal class
    AddSongToPlaylistWorkflowStartedEventHandler (IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<AddSongToPlaylistWorkflowStartedEvent>
{
    public async Task Handle(AddSongToPlaylistWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(
            request.ChatId,
            request.UserId,
            RadioFreeChatStates.AddSongToPlaylist);
    }
}

internal class
    AddSongToPlaylistWorkflowCompletedEventHandler (IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<AddSongToPlaylistWorkflowCompletedEvent>
{
    public async Task Handle(AddSongToPlaylistWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId, request.UserId);
    }
}
