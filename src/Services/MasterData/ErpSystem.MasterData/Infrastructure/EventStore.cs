using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Infrastructure;

public class MasterDataEventStoreDbContext : DbContext
{
    public DbSet<EventStream> EventStreams { get; set; } = null!;

    public MasterDataEventStoreDbContext(DbContextOptions<MasterDataEventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}
