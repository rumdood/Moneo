using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using Moneo.Common;
using RadioFreeBot.Configuration;
using RadioFreeBot.Models;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Features.FindSong;

[UserCommand(
    CommandKey = "/find",
    HelpDescription = @"Finds a song on YouTube Music. Usage: /find <song name>")]
public partial class FindSongRequest : UserRequestBase
{
    [UserCommandArgument(
        LongName = nameof(SongName),
        HelpText = @"The name of the song to find.")]
    public string SongName { get; }

    public FindSongRequest(CommandContext context) : base(context)
    {
        SongName = context.Args.Length > 0 ? string.Join(" ", context.Args) : "";
    }

    public FindSongRequest(long conversationId, ChatUser? user, string? songName) : base(conversationId, user, songName)
    {
        SongName = songName;
    }
}

internal sealed class FindSongHandler : IRequestHandler<FindSongRequest, MoneoCommandResult>
{
    private readonly ILogger<FindSongHandler> _logger;
    private readonly IYouTubeMusicProxyClient _client;
    private readonly RadioFreeDbContext _dbContext;

    private async Task<string?> GetPlaylistIdForConversationAsync(long conversationId,
        CancellationToken cancellationToken)
    {
        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId, cancellationToken);

        return playlist?.ExternalId;
    }

    public FindSongHandler(IYouTubeMusicProxyClient client, ILogger<FindSongHandler> logger,
        RadioFreeDbContext dbContext)
    {
        _client = client;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<MoneoCommandResult> Handle(FindSongRequest request, CancellationToken cancellationToken)
    {
        var playlistId = await GetPlaylistIdForConversationAsync(request.Context.ConversationId, cancellationToken);

        if (string.IsNullOrEmpty(playlistId))
        {
            _logger.LogError("No playlist found for conversation {ConversationId}", request.Context.ConversationId);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    $"Sorry @{request.Context.User?.ReferenceName}, I couldn't find a playlist for this conversation. Please create a playlist first."
            };
        }

        var searchResult = await _client.FindSongOnPlaylistAsync(request.SongName, playlistId, cancellationToken);

        // var searchResult = await _client.FindSongAsync(request.SongName, cancellationToken);
        if (!searchResult.IsSuccess || searchResult.Data == null)
        {
            _logger.LogError("Failed to find song: {Error}", searchResult.Message);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    $"Sorry @{request.Context.User?.ReferenceName}, I couldn't find what you were looking for. Are you sure the name is correct? You can also try adding in the name of the artist or album."
            };
        }

        var songLinks = searchResult.Data!
            .Select(song => Utilities.GetYouTubeMusicLinkForSong(song, true))
            .ToArray();
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            Format = TextFormat.Markdown,
            UserMessageText =
                $"Hey @{request.Context.User?.ReferenceName}, I found the following songs on the playlist:\n{string.Join("\n", songLinks)}"
        };
    }
}
