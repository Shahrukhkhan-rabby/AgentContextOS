using AgentContextOS.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgentContextOS.Data;

public sealed class AcosDbContext(DbContextOptions<AcosDbContext> options) : DbContext(options)
{
    public DbSet<ContextEvent> ContextEvents => Set<ContextEvent>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.HasKey(e => e.ProjectHash);

            entity.Property(e => e.ProjectHash)
                .HasMaxLength(64);
        });

        modelBuilder.Entity<ContextEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.ProjectHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Embedding)
                .HasColumnType("BLOB");

            entity.HasIndex(e => e.ProjectHash);

            entity.HasIndex(e => e.Timestamp);

            entity.HasIndex(e => new { e.ProjectHash, e.Timestamp });
        });
    }

    /// <summary>
    /// Loads the sqlite-vec extension and creates the virtual table for KNN search.
    /// Call once after EnsureCreated / migration.
    /// </summary>
    public void InitializeVectorExtension()
    {
        var connection = (SqliteConnection)Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        connection.LoadExtension("vec0");

        Database.ExecuteSqlRaw("""
            CREATE VIRTUAL TABLE IF NOT EXISTS vec_context_events USING vec0(
                id TEXT PRIMARY KEY,
                embedding float[768]
            )
            """);
    }
}
