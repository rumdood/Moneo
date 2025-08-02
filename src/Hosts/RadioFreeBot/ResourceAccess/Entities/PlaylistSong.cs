using System.ComponentModel.DataAnnotations.Schema;

namespace RadioFreeBot.ResourceAccess.Entities;

public class PlaylistSong : EntityBase
{
    [Column("playlist_id")]
    public long PlaylistId { get; set; }
    public Playlist Playlist { get; set; }

    [Column("song_id")]
    public long SongId { get; set; }
    public Song Song { get; set; }

    [Column("added_by_user_id")]
    public long? AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }
    
    [Column("added_at")]
    public DateTime AddedAt { get; set; }

    private PlaylistSong()
    {
    }
    
    public PlaylistSong(long playlistId, long songId, DateTime addedAt, long? addedByUserId = null)
    {
        PlaylistId = playlistId;
        SongId = songId;
        AddedByUserId = addedByUserId;
        AddedAt = addedAt;
    }

    public PlaylistSong(Playlist playlist, Song song, DateTime addedAt, long? addedByUserId = null)
    {
        Playlist = playlist;
        Song = song;
        AddedByUserId = addedByUserId;
        AddedAt = addedAt;
    }

    public PlaylistSong(long playlistId, Song song, DateTime addedAt, long? addedByUserId = null)
    {
        PlaylistId = playlistId;
        Song = song;
        AddedByUserId = addedByUserId;
        AddedAt = addedAt;
    }
}

