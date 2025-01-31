using Moneo.TaskManagement.DomainEvents;

namespace Moneo.TaskManagement.ResourceAccess;

public interface IHasDomainEvents
{
    List<DomainEvent> DomainEvents { get; }
}