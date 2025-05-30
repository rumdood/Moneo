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
}

