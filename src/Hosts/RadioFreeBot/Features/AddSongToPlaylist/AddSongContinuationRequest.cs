using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

[WorkflowContinuationCommand(nameof(RadioFreeChatStates.AddSongToPlaylist), "/continueAddSongToPlaylist")]
public partial class AddSongContinuationRequest : UserRequestBase
{
    public string Text { get; }
    
    public AddSongContinuationRequest(CommandContext context) : base(context)
    {
        Text = string.Join(" ", context.Args);
    }
    
    public AddSongContinuationRequest(long conversationId, ChatUser? user, string text) : base(conversationId, user)
    {
        Text = text;
    }
}

internal class AddSongContinuationRequestHandler : IRequestHandler<AddSongContinuationRequest, MoneoCommandResult>
{
    private readonly IAddSongByQueryWorkflowManager _addSongByQueryWorkflowManager;

    public AddSongContinuationRequestHandler(IAddSongByQueryWorkflowManager addSongByQueryWorkflowManager)
    {
        _addSongByQueryWorkflowManager = addSongByQueryWorkflowManager;
    }
    
    public Task<MoneoCommandResult> Handle(AddSongContinuationRequest request, CancellationToken cancellationToken)
        => _addSongByQueryWorkflowManager.ContinueWorkflowAsync(request.Context, request.Text, cancellationToken);
}