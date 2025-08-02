using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadioFreeBot.ResourceAccess.Entities;

public abstract class EntityBase : IEntity
{
    [Key]
    [Column("id")]
    public long Id { get; internal set; }
    [Column("created_on")]
    public DateTime CreatedOn { get; internal set; }
}