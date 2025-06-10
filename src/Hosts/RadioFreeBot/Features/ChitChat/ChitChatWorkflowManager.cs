using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.Chitchat;

namespace RadioFreeBot.Features.ChitChat;

[MoneoWorkflow]
public class ChitChatWorkflowManager : WorkflowManagerBase, IChitChatWorkflowManager
{
    public ChitChatWorkflowManager(IMediator mediator) : base(mediator)
    {
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default)
    {
        // we want to check to see if the user has sent us a link to a YouTube Music video
        // the format for such a link is: https://music.youtube.com/watch?v=VIDEO_ID
        // if they have, we want to get the video ID,
        // if they haven't, then we want to ignore the input and return an empty response
        if (userInput.StartsWith("https://music.youtube.com/watch?v="))
        {
            var videoId = userInput.Split("v=", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (videoId != null)
            {
                var response =
                    await Mediator.Send(
                        AddSongToPlaylist.AddSongRequest.CreateForSongId(cmdContext, videoId),
                        cancellationToken);
                return response;
            }
        }
        
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.None
        };
    }
}