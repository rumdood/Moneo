using System.Text;
using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using RadioFreeBot.Configuration;
using RadioFreeBot.YouTube;

namespace RadioFreeBot.Features.FindSong;

public class FindSongByYouTubeId : UserRequestBase
{
    public string VideoId { get; private set; }
    
    public FindSongByYouTubeId(CommandContext context) : base(context)
    {
        VideoId = context.Args.Length > 0 ? context.Args[0] : string.Empty;
    }

    public FindSongByYouTubeId(long conversationId, ChatUser? user, string? videoId) : base(conversationId, user, videoId)
    {
        VideoId = videoId ?? string.Empty;
    }
    
    public static FindSongByYouTubeId CreateForVideoId(CommandContext context, string videoId)
    {
        return new FindSongByYouTubeId(context)
        {
            VideoId = videoId
        };
    }
}

internal class FindSongByYouTubeIdHandler : IRequestHandler<FindSongByYouTubeId, MoneoCommandResult>
{
    private readonly IYouTubeMusicProxyClient _youtubeMusicProxyClient;
    private readonly IRadioFreeYouTubeService _youtubeService;
    private readonly ILogger<FindSongByYouTubeIdHandler> _logger;

    public FindSongByYouTubeIdHandler(IRadioFreeYouTubeService youtubeService, IYouTubeMusicProxyClient youtubeMusicProxyClient, ILogger<FindSongByYouTubeIdHandler> logger)
    {
        _youtubeMusicProxyClient = youtubeMusicProxyClient;
        _youtubeService = youtubeService;
        _logger = logger;
    }

    public async Task<MoneoCommandResult> Handle(FindSongByYouTubeId request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.VideoId))
        {
            return new MoneoCommandResult
            {
                Type = ResultType.Error,
                ResponseType = ResponseType.Text,
                UserMessageText = "Video ID cannot be null or empty."
            };
        }

        var videoRecord = await _youtubeService.GetVideoInfo(request.VideoId, cancellationToken);
        
        if (videoRecord is null)
        {
            return new MoneoCommandResult
            {
                Type = ResultType.Error,
                ResponseType = ResponseType.Text,
                UserMessageText = $"Video with ID {request.VideoId} not found."
            };
        }

        var messageBuilder = new StringBuilder($"@{request.Context.User?.ReferenceName} \\- I searched for your YouTube video:\n");
        messageBuilder.AppendLine(videoRecord.Title.EscapeMarkdown());
        
        var songQuery = $"{videoRecord.Title} {string.Join(" ", videoRecord.Tags)}";

        var ytmItems = await _youtubeMusicProxyClient.FindSongAsync(songQuery, cancellationToken);
        if (!ytmItems.IsSuccess || ytmItems.Data == null || ytmItems.Data.Count == 0)
        {
            messageBuilder.AppendLine("However, I couldn't find it on YouTube Music.");
            _logger.LogError("Failed to find song on YouTube Music: {Error}", ytmItems.Message);
            return new MoneoCommandResult
            {
                Type = ResultType.Error,
                ResponseType = ResponseType.Text,
                UserMessageText = messageBuilder.ToString()
            };
        }
        
        // Example: Sort by how well the song title matches the video title
        var sortedItems = ytmItems.Data
            .OrderByDescending(item => 
                string.Equals(item.Title, videoRecord.Title, StringComparison.OrdinalIgnoreCase) ? 2 :
                item.Title.Contains(videoRecord.Title, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ToList();
        
        messageBuilder.AppendLine($"I found the following options on YouTube Music:");
        foreach (var item in sortedItems)
        {
            messageBuilder.AppendLine($"\\- {Utilities.GetYouTubeMusicLinkForSong(item, true)}");
        }
        
        return new MoneoCommandResult
        {
            Type = ResultType.WorkflowCompleted,
            ResponseType = ResponseType.Text,
            UserMessageText = messageBuilder.ToString(),
            Format = TextFormat.Markdown,
        };
    }
}
