using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("task_events")]
public class TaskEvent : AuditableEntity
{
    [Column("task_id")]
    public long TaskId { get; internal set; }
    
    public MoneoTask Task { get; internal set; }
    
    [Required]
    [Column("occurred_on")]
    public DateTimeOffset OccurredOn { get; internal set; }
    
    [Required]
    [Column("type")]
    public TaskEventType Type { get; internal set; }
    
    [Column("data_json")]
    public string? DataJson { get; internal set; }
    
    private TaskEvent() { }
    
    public TaskEvent(MoneoTask task, TaskEventType type, DateTimeOffset occurredOn)
    {
        Task = task;
        OccurredOn = occurredOn;
        Type = type;
    }
}