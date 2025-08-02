using Moneo.Chat;

namespace RadioFreeBot.Models;

public sealed class RadioFreeChatStates : ChatStateProviderBase
{
    public static readonly ChatState AddSongToPlaylist = ChatState.Register(nameof(AddSongToPlaylist));
    public static readonly ChatState RemoveSongFromPlaylist = ChatState.Register(nameof(RemoveSongFromPlaylist));
}