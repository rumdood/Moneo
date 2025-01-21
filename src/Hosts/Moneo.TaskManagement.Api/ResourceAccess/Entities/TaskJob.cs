using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("task_jobs")]
public class TaskJob : AuditableEntity
{
    [Required]
    [Column("task_id")]
    public long TaskId { get; internal set; }
    
    [Required]
    [Column("job_id")]
    public long JobId { get; internal set; }
    
    public MoneoTask Task { get; internal set; }
}