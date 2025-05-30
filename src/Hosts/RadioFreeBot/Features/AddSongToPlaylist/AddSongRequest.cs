using Moneo.Chat;
using Moneo.Chat.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

[UserCommand(
    CommandKey = "/add",
    HelpDescription = "Add a song to the playlist. If the song is not already on the playlist, it will be added to the end.")]
public partial class AddSongRequest : UserRequestBase
{
    [UserCommandArgument(
        LongName = nameof(SongQuery),
        HelpText = @"The information to find the song you want to add. Usually something like the song name and the artist name.

ex: /add Jump by Van Halen")]
    public string SongQuery { get; set; }

    public string PlaylistId { get; set; } = "";

    internal string SongId { get; init; } = "";
    
    public AddSongRequest(CommandContext context) : base(context)
    {
        if (context.Args.Length == 0)
            throw new ArgumentException("SongQuery cannot be null or empty", nameof(context));
        
        SongQuery = string.Join(" ", context.Args);
    }
    
    public AddSongRequest(long conversationId, ChatUser? user, string? songQuery) : base(conversationId, user)
    {
        SongQuery = songQuery;
    }

    internal static AddSongRequest CreateForSongId(CommandContext context, string songId)
    {
        return new AddSongRequest(context)
        {
            SongId = songId
        };
    }
}