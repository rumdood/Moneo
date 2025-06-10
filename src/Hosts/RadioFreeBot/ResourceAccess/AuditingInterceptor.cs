using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.ResourceAccess;

public class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public AuditingInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);
            
        foreach (var entry in eventData.Context.ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = _timeProvider.GetUtcNow().UtcDateTime;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
