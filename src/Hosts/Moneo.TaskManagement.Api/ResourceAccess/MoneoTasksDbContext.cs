using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.ResourceAccess;

public class MoneoTasksDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<UserConversation> UserConversations { get; set; }
    public DbSet<MoneoTask> MoneoTasks { get; set; }
    public DbSet<TaskRepeater> TaskRepeaters { get; set; }
    public DbSet<TaskEvent> TaskEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(u => u.Property(p => p.Email).IsRequired());
        modelBuilder.Entity<MoneoTask>(t => 
        {
            t.Property(p => p.Name).IsRequired();
            t.Property(p => p.Description).IsRequired();
            t.HasIndex(p => new { p.Name, p.ConversationId }).IsUnique();
        });
        modelBuilder.Entity<Conversation>(c => c.Property(p => p.Transport).IsRequired());
        modelBuilder.Entity<TaskEvent>(e =>
        {
            e.Property(p => p.Timestamp).IsRequired();
            e.Property(p => p.Type).IsRequired();
            e.HasOne(p => p.Task)
                .WithMany(t => t.TaskEvents)
                .HasForeignKey(te => te.TaskId);
        });
        base.OnModelCreating(modelBuilder);
    }
}