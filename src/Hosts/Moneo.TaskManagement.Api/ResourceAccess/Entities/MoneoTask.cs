using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("tasks")]
public class MoneoTask
{
    [Key]
    [Column("id")]
    public long Id { get; internal set; }
    
    [Column("name")]
    public string Name { get; internal set; }
    
    [Column("description")]
    public string Description { get; internal set; }

    [Column("isActive")]
    public bool IsActive { get; internal set; } = true;
    
    [Column("completedMessages")]
    public string CompletedMessages { get; internal set; }

    [Column("canBeSkipped")] 
    public bool CanBeSkipped { get; internal set; } = true;
    
    [Column("skippedMessages")]
    public string SkippedMessages { get; internal set; }

    [Column("timezone")]
    public string Timezone { get; internal set; } = "";
    
    [Column("dueOn")]
    public DateTime? DueOn { get; internal set; }
    
    [Column("badgerFrequency")]
    public int? BadgerFrequencyInMinutes { get; internal set; }
    
    [Column("badgerMessages")]
    public string? BadgerMessages { get; internal set; }
    
    [Column("createdOn")]
    public DateTime CreatedOn { get; internal set; }
    
    [Column("modifiedOn")]
    public DateTime ModifiedOn { get; internal set; }
    
    [Column("conversation_id")]
    public long ConversationId { get; internal set; }
    
    public Conversation Conversation { get; internal set; }
    
    public TaskRepeater? TaskRepeater { get; internal set; }
    public ICollection<TaskEvent> TaskEvents { get; internal set; } = new List<TaskEvent>();
}