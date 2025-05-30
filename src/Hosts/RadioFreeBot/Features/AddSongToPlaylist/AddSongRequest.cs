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
    
    public AddSongRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        if (args.Length == 0)
            throw new ArgumentException("SongQuery cannot be null or empty", nameof(args));
        
        SongQuery = string.Join(" ", args);
    }
    
    public AddSongRequest(long conversationId, ChatUser? user, string? songQuery) : base(conversationId, user)
    {
        SongQuery = songQuery;
    }

    internal static AddSongRequest CreateForSongId(long conversationId, ChatUser? user, string songId)
    {
        return new AddSongRequest(conversationId,  user, "")
        {
            SongId = songId
        };
    }
}