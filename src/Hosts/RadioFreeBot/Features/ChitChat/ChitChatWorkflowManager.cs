using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.Chitchat;
using RadioFreeBot.Configuration;
using RadioFreeBot.Features.FindSong;

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
        if (userInput.StartsWith(Utilities.Constants.YouTubeSongUrl))
        {
            var videoId = userInput.Split("v=", StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Split('&').FirstOrDefault()?.Trim();
            if (videoId != null)
            {
                var response =
                    await Mediator.Send(
                        AddSongToPlaylist.AddSongRequest.CreateForSongId(cmdContext, videoId),
                        cancellationToken);
                return response;
            }
        }

        if (userInput.StartsWith(Utilities.Constants.YouTubeShortVideoUrl, StringComparison.OrdinalIgnoreCase))
        {
            // convert the YouTube URL to a URI
            var uri = new Uri(userInput);
            
            // get the video ID from the URI
            var videoId = uri.Segments.LastOrDefault()?.Trim('/').Split('?').FirstOrDefault()?.Trim();
            
            if (videoId != null)
            {
                // send the request to add the song to the playlist
                var response =
                    await Mediator.Send(
                        FindSongByYouTubeId.CreateForVideoId(cmdContext, videoId),
                        cancellationToken);
                return response;
            }
        }

        if (userInput.StartsWith(Utilities.Constants.YouTubeLongVideoUrl, StringComparison.OrdinalIgnoreCase) ||
            userInput.StartsWith(Utilities.Constants.YouTubeMobileVideoUrl, StringComparison.OrdinalIgnoreCase))
        {
            var videoId = userInput.Split("v=", StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Split('&').FirstOrDefault()?.Trim();
            if (videoId != null)
            {
                var response =
                    await Mediator.Send(
                        FindSongByYouTubeId.CreateForVideoId(cmdContext, videoId),
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