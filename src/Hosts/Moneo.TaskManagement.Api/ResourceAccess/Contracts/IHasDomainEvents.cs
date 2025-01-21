using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.ResourceAccess;

public interface IHasDomainEvents
{
    List<DomainEvent> DomainEvents { get; }
}