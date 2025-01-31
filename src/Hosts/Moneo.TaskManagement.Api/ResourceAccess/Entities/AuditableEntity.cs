using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

public abstract class AuditableEntity : IAuditable, IEntity
{
    [Key]
    [Column("id")]
    public long Id { get; internal set; }
    
    [Column("created_on")]
    public DateTime CreatedOn { get; set; }
    [Column("created_by")]
    public string? CreatedBy { get; set; }
    [Column("modified_on")]
    public DateTime? ModifiedOn { get; set; }
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }
}