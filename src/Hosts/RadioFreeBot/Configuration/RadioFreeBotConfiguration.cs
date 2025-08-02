namespace RadioFreeBot.Configuration;

public class RadioFreeBotConfiguration
{
    public YouTubeMusicProxyOptions YouTubeMusicProxy { get; set; }
    public YouTubeVideosOptions YouTubeVideos { get; set; }
    public bool LoadPlaylistsFromYouTubeOnStartup { get; set; } = false;
}

public class YouTubeVideosOptions
{
    public string ApiKey { get; set; }
}
