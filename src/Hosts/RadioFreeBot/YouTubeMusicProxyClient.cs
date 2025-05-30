using Moneo.Common;
using Moneo.Web;
using RadioFreeBot.Models;

namespace RadioFreeBot;

public interface IYouTubeMusicProxyClient
{
    Task<MoneoResult<List<SongItem>>> FindSongAsync(string query, CancellationToken cancellationToken = default);
    Task<MoneoResult> AddSongToPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default);
    Task<MoneoResult> RemoveSongFromPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default);
    Task<MoneoResult<SongItem>> GetSongFromPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default);
}

public class YouTubeProxyOptions
{
    public string YouTubeMusicProxyUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

internal class YouTubeMusicProxyClient : IYouTubeMusicProxyClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public YouTubeMusicProxyClient(HttpClient httpClient, YouTubeProxyOptions options)
    {
        _httpClient = httpClient;
        _baseUrl = options.YouTubeMusicProxyUrl;
    }
    
    private async Task<MoneoResult<T>> SendRequestAsync<T>(HttpMethod method, string uri, object? content = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(new Uri(_baseUrl), uri);
        var requestMessage = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(content)
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            requestMessage.Headers.Add("X-Api-Key", _apiKey);
        }

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var result = await response.GetMoneoResultAsync<T>(cancellationToken);
        return result;
    }
    
    public Task<MoneoResult<List<SongItem>>> FindSongAsync(string query, CancellationToken cancellationToken = default)
    {
        return SendRequestAsync<List<SongItem>>(HttpMethod.Get, $"song/find/{query}", null, cancellationToken);
    }

    public async Task<MoneoResult> AddSongToPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Post, $"playlist/{playlistId}/add/{songId}", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult> RemoveSongFromPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Delete, $"playlist/{playlistId}/remove/{songId}", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public Task<MoneoResult<SongItem>> GetSongFromPlaylistAsync(string playlistId, string songId, CancellationToken cancellationToken = default)
    {
        return SendRequestAsync<SongItem>(HttpMethod.Get, $"playlist/{playlistId}/song/{songId}", null, cancellationToken);
    }
}