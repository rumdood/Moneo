using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadioFreeBot.ResourceAccess.Entities;

public class Playlist : EntityBase
{
    [Required]
    [StringLength(100)]
    [Column("external_id")]
    public string ExternalId { get; internal set; }
    
    [Required]
    [StringLength(250)]
    [Column("name")]
    public string Name { get; internal set; }
    
    [Required]
    [Column("conversation_id")]
    public long ConversationId { get; internal set; }
    
    public ICollection<PlaylistSong> PlaylistSongs { get; internal set; } = new List<PlaylistSong>();

    private Playlist() { } // for EF Core
    
    public Playlist(string externalId, string name, long conversationId)
    {
        ExternalId = externalId;
        Name = name;
        ConversationId = conversationId;
    }
}