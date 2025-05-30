using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace RadioFreeBot.Features.FindSong;

[UserCommand(
    CommandKey="/find",
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
    private const string YouTubeSongUrl = "https://music.youtube.com/watch?v=";

    public FindSongHandler(IYouTubeMusicProxyClient client, ILogger<FindSongHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<MoneoCommandResult> Handle(FindSongRequest request, CancellationToken cancellationToken)
    {
        var searchResult = await _client.FindSongAsync(request.SongName, cancellationToken);
        if (!searchResult.IsSuccess || searchResult.Data == null)
        {
            _logger.LogError("Failed to find song: {Error}", searchResult.Message);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"Sorry @{request.Context.User?.Username}, I couldn't find what you were looking for. Are you sure the name is correct? You can also try adding in the name of the artist or album."
            };
        }

        var songLinks = searchResult.Data!.Select(song => YouTubeSongUrl + song.Id).ToArray();
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = $"Hey @{request.Context.User?.Username}, I found the following songs:\n{string.Join("\n", songLinks)}"
        };
    }
}
