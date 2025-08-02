using Moneo.Chat;
using Moneo.Chat.Models;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

[UserCommand(
    CommandKey = "/remove",
    HelpDescription = "Removes a song from the playlist")]
public partial class RemoveSongFromPlaylistRequest : UserRequestBase
{
    [UserCommandArgument(HelpText = "The name of the song to remove from the playlist. You can also include the name of the artist and/or album to help disambiguate if there are multiple songs with the same name.")]
    public string SongName { get; init; } = string.Empty;
    
    public RemoveSongFromPlaylistRequest(CommandContext context) : base(context)
    {
        if (context.Args.Length > 0)
        {
            SongName = string.Join(' ', context.Args);
        }
    }
    
    public RemoveSongFromPlaylistRequest(CommandContext context, string songName) : base(context)
    {
        SongName = songName;
    }

    public RemoveSongFromPlaylistRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        if (args.Length > 0)
        {
            SongName = string.Join(' ', args);
        }
    }
}
