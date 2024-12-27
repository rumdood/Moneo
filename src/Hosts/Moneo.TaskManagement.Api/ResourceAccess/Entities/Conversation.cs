using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("conversations")]
public class Conversation
{
    [Key]
    [Column("id")]
    public long Id { get; internal set; }
    
    [Column("transport")]
    public Transport Transport { get; internal set; }
    
    public ICollection<UserConversation> UserConversations { get; internal set; } = new List<UserConversation>();
    public ICollection<MoneoTask> Tasks { get; internal set; } = new List<MoneoTask>();
}