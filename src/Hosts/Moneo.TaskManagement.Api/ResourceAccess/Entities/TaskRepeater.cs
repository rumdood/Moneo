using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("task_repeaters")]
public class TaskRepeater
{
    [Column("expiry")]
    public DateTime? Expiry { get; set; }

    [Column("repeatCron")]
    public string RepeatCron { get; set; } = "";

    [Column("earlyCompletionThreshold")]
    public int? EarlyCompletionThresholdHours { get; set;}
    
    [Column("task_id")]
    public long? TaskId { get; set; }
    
    public MoneoTask? Task { get; set; }
}