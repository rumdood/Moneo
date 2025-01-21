using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("users")]
public class User : AuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; internal set; }
    
    [Column("name")]
    public string Name { get; internal set; }
    
    [Required]
    [Column("email")]
    public string Email { get; internal set; }
    
    public ICollection<UserConversation> UserConversations { get; internal set; } = new List<UserConversation>();
}