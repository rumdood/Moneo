using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess.Entities;
using TaskEvent = Moneo.TaskManagement.ResourceAccess.Entities.TaskEvent;

namespace Moneo.TaskManagement.ResourceAccess;

public class MoneoTasksDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;
    private IDbContextTransaction? _currentTransaction;
    
    public DbSet<User> Users { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<UserConversation> UserConversations { get; set; }
    public DbSet<MoneoTask> Tasks { get; set; }
    public DbSet<TaskEvent> TaskEvents { get; set; }

    public MoneoTasksDbContext(DbContextOptions<MoneoTasksDbContext> options, TimeProvider timeProvider, IMediator mediator)
        : base(options)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);

            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
    
    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = _timeProvider.GetUtcNow().UtcDateTime;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedOn = _timeProvider.GetUtcNow().UtcDateTime;
                    break;
                case EntityState.Deleted:
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }

        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count != 0)
            .ToArray();

        var result = await base.SaveChangesAsync(cancellationToken);
        
        // we're going to try firing the events after the transaction is committed so we don't 
        // have to worry about the events being dispatched if the transaction is rolled back
        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToArray();
            entity.DomainEvents.Clear();
            await DispatchEvents(events);
        }
        
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // for each entity that implements IHasDomainEvents, configure the DomainEvents property to be ignored by EF
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IHasDomainEvents).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }
        
        modelBuilder.Entity<MoneoTask>()
            .HasOne(t => t.Conversation)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<UserConversation>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.UserConversations)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserConversation>()
            .HasKey(c => new { UserId = c.UserId, ConversationId = c.ConversationId });
        
        modelBuilder.Entity<TaskEvent>()
            .HasOne(te => te.Task)
            .WithMany(t => t.TaskEvents)
            .HasForeignKey(te => te.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<TaskJob>()
            .HasOne(tj => tj.Task)
            .WithMany(t => t.TaskJobs)
            .HasForeignKey(tj => tj.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        
        CreateIndexes(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }
    
    private static void CreateIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email, "IDX_User_Email")
            .IsUnique();
        
        modelBuilder.Entity<MoneoTask>()
            .HasIndex(t => new { t.Name, t.ConversationId }, "IDX_Task_Name_ConversationId")
            .IsUnique();
        
        modelBuilder.Entity<TaskEvent>()
            .HasIndex(t => t.OccurredOn, "IDX_TaskEvent_OccurredOn");
    }
    
    private async Task DispatchEvents(IReadOnlyList<DomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}