using RadioFreeBot.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

public enum SongHistoryType
{
    Added,
    Removed,
}

public class UserPlaylistHistory
{
    public SongItem Song { get; init; }
    public DateTime OccurredOn { get; init; }
    public SongHistoryType HistoryType { get; init; }
    
    public UserPlaylistHistory(SongItem song, DateTime occurredOn, SongHistoryType historyType)
    {
        Song = song;
        OccurredOn = occurredOn;
        HistoryType = historyType;
    }
}

public class UserWithHistory
{
    public long TelegramId { get; init; }
    public string Name { get; init; }
    public IReadOnlyList<UserPlaylistHistory> History { get; init; }
    
    public UserWithHistory(long telegramId, string name, IReadOnlyList<UserPlaylistHistory> history)
    {
        TelegramId = telegramId;
        Name = name;
        History = history;
    }
}