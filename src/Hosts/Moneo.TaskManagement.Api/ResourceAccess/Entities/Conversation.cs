using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("conversations")]
public class Conversation : AuditableEntity, IHasDomainEvents
{
    [Required]
    [Column("transport")]
    public Transport Transport { get; internal set; }
    
    public ICollection<UserConversation> UserConversations { get; internal set; } = new List<UserConversation>();
    public ICollection<MoneoTask> Tasks { get; internal set; } = new List<MoneoTask>();
    
    public List<DomainEvent> DomainEvents { get; set; }
    
    private Conversation() { }
    
    public Conversation(Transport transport)
    {
        Transport = transport;
    }
}