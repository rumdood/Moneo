using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace RadioFreeBot.YouTube;

public record YouTubeVideoRecord(string VideoId, string Title, IReadOnlyList<string> Tags, string? Description);

public interface IRadioFreeYouTubeService
{
    Task<YouTubeVideoRecord?> GetVideoInfo(string videoId, CancellationToken cancellationToken = default);
}

internal class RadioFreeYouTubeService : IRadioFreeYouTubeService
{
    private YouTubeService? _youtube = null;
    private readonly ILogger<RadioFreeYouTubeService> _logger;

    public RadioFreeYouTubeService(ILogger<RadioFreeYouTubeService> logger)
    {
        _logger = logger;
    }
    
    public async Task<YouTubeVideoRecord?> GetVideoInfo(string videoId, CancellationToken cancellationToken = default)
    {
        _youtube ??= new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = "AIzaSyB7pie6BU_1qKMyDFemiMo-NSoqHmTYRZM",
            ApplicationName = "badgerbot"
        });

        var videoRequest = _youtube.Videos.List("snippet");
        videoRequest.Id = videoId;
        
        _logger.LogDebug("Sending YouTube Video Request {@VideoRequest}", videoRequest);
        
        var response = await videoRequest.ExecuteAsync(cancellationToken);
        
        if (response.Items.Count == 0)
        {
            throw new KeyNotFoundException($"Video with ID {videoId} not found.");
        }
        
        _logger.LogDebug("Received YouTube Video Response {@Response}", response);

        return response.Items.FirstOrDefault()?.ToYouTubeVideoRecord();
    }
}

internal static class YouTubeVideoExtensions
{
    public static YouTubeVideoRecord ToYouTubeVideoRecord(this Google.Apis.YouTube.v3.Data.Video video)
    {
        return new YouTubeVideoRecord(
            video.Id,
            video.Snippet.Title,
            video.Snippet.Tags?.ToArray() ?? [], 
            video.Snippet.Description);
    }
}
