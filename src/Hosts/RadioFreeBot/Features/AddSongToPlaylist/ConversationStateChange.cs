using MediatR;
using Moneo.Chat;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

public record AddSongToPlaylistWorkflowStartedEvent(long ChatId) : IRequest;
public record AddSongToPlaylistWorkflowCompletedEvent(long ChatId) : IRequest;

internal class
    AddSongToPlaylistWorkflowStartedEventHandler (IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<AddSongToPlaylistWorkflowStartedEvent>
{
    public async Task Handle(AddSongToPlaylistWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(
            request.ChatId,
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
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}
