using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.ResourceAccess;

public class RadioFreeDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;
    private IDbContextTransaction? _currentTransaction;
    
    public DbSet<Playlist> Playlists { get; set; }

    public RadioFreeDbContext(DbContextOptions<RadioFreeDbContext> options, TimeProvider timeProvider, IMediator mediator) 
        : base(options)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added) 
            {
                entry.Entity.CreatedOn = _timeProvider.GetUtcNow().UtcDateTime;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlaylistSong>()
            .HasOne(ps => ps.Playlist)
            .WithMany(p => p.PlaylistSongs)
            .HasForeignKey(ps => ps.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<PlaylistSong>()
            .HasOne(ps => ps.Song)
            .WithMany(s => s.PlaylistSongs)
            .HasForeignKey(ps => ps.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlaylistSong>()
            .HasOne(ps => ps.AddedByUser)
            .WithMany(u => u.PlaylistSongs)
            .HasForeignKey(ps => ps.AddedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlaylistSong>()
            .HasAlternateKey(ps => new { ps.PlaylistId, ps.SongId });
        
        base.OnModelCreating(modelBuilder);
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
}