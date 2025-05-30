namespace RadioFreeBot.ResourceAccess.Entities;

public interface IEntity
{
    public long Id { get; }
    public DateTime CreatedOn { get; }
}