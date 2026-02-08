using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Maintenance.Infrastructure;

public class MaintenanceEventStoreDbContext : DbContext
{
    public DbSet<EventStream> Events { get; set; } = null!;

    public MaintenanceEventStoreDbContext(DbContextOptions<MaintenanceEventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class MaintenanceReadDbContext : DbContext
{
    public MaintenanceReadDbContext(DbContextOptions<MaintenanceReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add read model mappings here
    }
}
