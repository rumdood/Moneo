using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Features.GetHistory;

[UserCommand("/gethistory", "Get playlist history for a user")]
public partial class GetHistoryChatRequest : UserRequestBase
{
    [UserCommandArgument(nameof(UserName), helpText: "The name of the user to get history for.")]
    public string UserName { get; init; }

    public GetHistoryChatRequest(CommandContext context) : base(context)
    {
        if (context.Args is null || context.Args.Length == 0)
        {
            throw new ArgumentException("User name is required.", nameof(context.Args));
        }

        UserName = context.Args.First();
    }

    public GetHistoryChatRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user,
        args)
    {
        if (args is null || args.Length == 0)
        {
            throw new ArgumentException("User name is required.", nameof(args));
        }

        UserName = args.First();
    }
}

internal class GetHistoryChatRequestHandler : IRequestHandler<GetHistoryChatRequest, MoneoCommandResult>
{
    private readonly RadioFreeDbContext _dbContext;
    private readonly ILogger<GetHistoryChatRequestHandler> _logger;
    private const string YouTubeSongUrl = "https://music.youtube.com/watch?v=";

    public GetHistoryChatRequestHandler(RadioFreeDbContext dbContext, ILogger<GetHistoryChatRequestHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var charsToEscape = new[]
            { "\\", "`", "*", "_", "{", "}", "[", "]", "(", ")", "#", "+", "-", ".", "!", "|", ">", "~", "=" };
        return charsToEscape.Aggregate(text, (current, c) => current.Replace(c, "\\" + c));
    }

    public async Task<MoneoCommandResult> Handle(GetHistoryChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var playlist =
                _dbContext.Playlists.FirstOrDefault(pl => pl.ConversationId == request.Context.ConversationId);
            if (playlist == null)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = "No playlist found for this conversation."
                };
            }

            var user = await _dbContext.GetUserPlaylistHistoryForTelegramAsync(
                playlist.Id,
                userName: request.UserName,
                cancellationToken: cancellationToken);

            if (user == null || user.History.Count == 0)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = $"No history found for user {request.UserName}."
                };
            }

            var songLinks = user.History
                .Select(h => $"*{EscapeMarkdown(h.OccurredOn.ToString("f"))}:*  [{EscapeMarkdown(h.Song.Title)}]({YouTubeSongUrl + h.Song.Id})")
                .ToArray();

            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"History for user {request.UserName}:\n" +
                                  string.Join("\n",
                                      songLinks),
                Format = TextFormat.Markdown,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving history for user {UserName}", request.UserName);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"Failed to retrieve history for user {request.UserName}: {e.Message}"
            };
        }
    }
}