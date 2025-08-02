using RadioFreeBot.Models;

namespace RadioFreeBot.Configuration;

internal static class Utilities
{
    public static class Constants
    {
        public const string YouTubeSongUrl = "https://music.youtube.com/watch?v=";
        public const string YouTubeLongVideoUrl = "https://www.youtube.com/watch?v=";
        public const string YouTubeMobileVideoUrl = "https://m.youtube.com/watch?v=";
        public const string YouTubeShortVideoUrl = "https://youtu.be/";
    }
    
    public static HashSet<string> YouTubeVideoUrls =
    [
        Constants.YouTubeLongVideoUrl,
        Constants.YouTubeShortVideoUrl
    ];
    
    public static string GetYouTubeVideoUrl(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID cannot be null or empty.", nameof(videoId));
        }
        
        return $"{Constants.YouTubeLongVideoUrl}{videoId}";
    }

    public static string GetYouTubeMusicSongUrl(string songId)
    {
        if (string.IsNullOrWhiteSpace(songId))
        {
            throw new ArgumentException("Song ID cannot be null or empty.", nameof(songId));
        }
        
        return $"{Constants.YouTubeSongUrl}{songId}";
    }

    public static string GetYouTubeMusicLinkForSong(SongItem song, bool escapeMarkdown = false)
    {
        return escapeMarkdown
            ? $"[{song.Title.EscapeMarkdown()}]({Constants.YouTubeSongUrl + song.Id}) by {song.Artist.EscapeMarkdown()}"
            : $"[{song.Title}]({Constants.YouTubeSongUrl + song.Id}) by {song.Artist}";
    }

    public static string GetExternalIdFromYouTubeMusicUrl(string url)
    {
        // get the url from the YouTube Music URL
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }
        
        if (!url.StartsWith(Constants.YouTubeSongUrl, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The provided URL is not a valid YouTube Music URL.");
        }
        
        return url.Substring(Constants.YouTubeSongUrl.Length);
    }
}

public static class StringExtensions
{
    public static string EscapeMarkdown(this string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var charsToEscape = new[] { "\\", "`", "*", "_", "{", "}", "[", "]", "(", ")", "#", "+", "-", ".", "!", "|", ">", "~", "=" };
        return charsToEscape.Aggregate(text, (current, c) => current.Replace(c, "\\" + c));
    }
}
