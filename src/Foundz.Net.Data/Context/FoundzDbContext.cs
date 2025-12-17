using Foundz.Net.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Foundz.Net.Data.Context;

/// <summary>
/// Database context for Foundz.Net.
/// </summary>
public class FoundzDbContext : DbContext
{
    public FoundzDbContext(DbContextOptions<FoundzDbContext> options) 
        : base(options)
    {
    }

    public DbSet<SessionEntity> Sessions { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<ToolExecutionEntity> ToolExecutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<SessionEntity>()
            .HasMany(s => s.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageEntity>()
            .HasMany(m => m.ToolExecutions)
            .WithOne(t => t.Message)
            .HasForeignKey(t => t.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        modelBuilder.Entity<SessionEntity>()
            .HasIndex(s => s.UserId);

        modelBuilder.Entity<SessionEntity>()
            .HasIndex(s => s.CreatedAt);

        modelBuilder.Entity<MessageEntity>()
            .HasIndex(m => m.SessionId);

        modelBuilder.Entity<MessageEntity>()
            .HasIndex(m => m.Timestamp);

        modelBuilder.Entity<ToolExecutionEntity>()
            .HasIndex(t => t.MessageId);
    }
}
