using System.ComponentModel.DataAnnotations.Schema;

namespace RadioFreeBot.ResourceAccess.Entities;

public class PlaylistSongSearch : EntityBase
{
    public string Content { get; set; }
    [Column(nameof(PlaylistSongSearch))]
    public string Match { get; set; }
    public double? Rank { get; set; }
    [Column("song_id")]
    public long SongId { get; set; }
    public Song Song { get; set; }

    private PlaylistSongSearch()
    {
    }
    
    public PlaylistSongSearch(string content, long songId)
    {
        Content = content;
        SongId = songId;
    }
    
    public PlaylistSongSearch(Song song)
    {
        Content = song.Name;
        Song = song;
        SongId = song.Id;
        Match = "";
    }
}