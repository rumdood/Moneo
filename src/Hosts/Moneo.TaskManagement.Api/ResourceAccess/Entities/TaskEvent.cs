using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

public enum TaskEventType
{
    Completed = 1,
    Skipped = 2,
    Disabled = 3,
}

[Table("task_events")]
public class TaskEvent
{
    [Column("task_id")]
    public long TaskId { get; internal set; }
    [Column("timestamp")]
    public DateTime Timestamp { get; internal set; }
    [Column("type")]
    public TaskEventType Type { get; internal set; }
    
    public MoneoTask Task { get; internal set; }
}