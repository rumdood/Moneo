using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    
    private Song() { } // for EF Core
    
    public Song(string name, string? externalId = null, string originalUrl = "")
    {
        Name = name;
        ExternalId = externalId;
        OriginalUrl = originalUrl;
    }

    public Song(string name, Uri originalUrl)
    {
        Name = name;
        OriginalUrl = originalUrl.ToString();
    }

    public Song(string originalUrl)
    {
        OriginalUrl = originalUrl;
    }
}