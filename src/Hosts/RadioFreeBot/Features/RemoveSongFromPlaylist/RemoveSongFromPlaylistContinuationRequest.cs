using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

[WorkflowContinuationCommand(commandKey: "/continueRemoveSongFromPlaylist", stateName: nameof(RadioFreeChatStates.RemoveSongFromPlaylist))]
public partial class RemoveSongFromPlaylistContinuationRequest : UserRequestBase
{
    public string UserInput { get; }
    
    public RemoveSongFromPlaylistContinuationRequest(CommandContext context) : base(context)
    {
        UserInput = context.Args.Length > 0 ? string.Join(' ', context.Args) : string.Empty;
    }

    public RemoveSongFromPlaylistContinuationRequest(long conversationId, ChatUser? user, string userInput) : base(conversationId, user, userInput)
    {
        UserInput = userInput;
    }
}

internal class RemoveSongFromPlaylistContinuationRequestHandler : IRequestHandler<RemoveSongFromPlaylistContinuationRequest, MoneoCommandResult>
{
    private readonly IRemoveSongFromPlaylistWorkflowManager _workflowManager;

    public RemoveSongFromPlaylistContinuationRequestHandler(IRemoveSongFromPlaylistWorkflowManager workflowManager)
    {
        _workflowManager = workflowManager;
    }
    
    public Task<MoneoCommandResult> Handle(RemoveSongFromPlaylistContinuationRequest request, CancellationToken cancellationToken)
    {
        return _workflowManager.ContinueWorkflowAsync(request.Context, request.UserInput, cancellationToken);
    }
}