using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Identity.Infrastructure;

public class EventStoreDbContext : DbContext
{
    public DbSet<EventStream> EventStreams { get; set; } = null!;

    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}
