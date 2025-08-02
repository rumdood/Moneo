using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RadioFreeBot.Configuration;

namespace RadioFreeBot.ResourceAccess.Entities;

public class Song : EntityBase
{
    [Column("external_id")]
    public string? ExternalId { get; set; }
    
    [Column("name")]
    [StringLength(250)]
    public string Name { get; set; }
    
    [Required]
    [Column("original_url")]
    public string OriginalUrl { get; set; }
    
    public ICollection<PlaylistSong> PlaylistSongs = new List<PlaylistSong>();

    public ICollection<Artist> Artists { get; set; } = new List<Artist>();
    
    public ICollection<Album> Albums { get; set; } = new List<Album>();
    
    private Song() { } // for EF Core
    
    public Song(string name, string? externalId = null)
    {
        Name = name;
        ExternalId = externalId;
        OriginalUrl = string.IsNullOrEmpty(externalId) ? "" : Utilities.GetYouTubeMusicSongUrl(externalId);
    }

    public Song(string name, Uri originalUrl)
    {
        Name = name;
        OriginalUrl = originalUrl.ToString();
        ExternalId = Utilities.GetExternalIdFromYouTubeMusicUrl(OriginalUrl);
    }

    public Song(string originalUrl)
    {
        OriginalUrl = originalUrl;
        ExternalId = Utilities.GetExternalIdFromYouTubeMusicUrl(OriginalUrl);
    }
}