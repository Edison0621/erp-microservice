using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Sales.Infrastructure;

public class SalesEventStoreDbContext : DbContext
{
    public DbSet<EventStream> Events { get; set; } = null!;

    public SalesEventStoreDbContext(DbContextOptions<SalesEventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class SalesReadDbContext : DbContext
{
    public DbSet<SalesOrderReadModel> SalesOrders { get; set; } = null!;
    public DbSet<ShipmentReadModel> Shipments { get; set; } = null!;

    public SalesReadDbContext(DbContextOptions<SalesReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrderReadModel>().Property(x => x.Lines).HasColumnType("jsonb");
        modelBuilder.Entity<ShipmentReadModel>().Property(x => x.Lines).HasColumnType("jsonb");
    }
}

public class SalesOrderReadModel
{
    public Guid Id { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "CNY";
    public decimal TotalAmount { get; set; }
    public string Lines { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
}

public class ShipmentReadModel
{
    public Guid Id { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public DateTime ShippedDate { get; set; }
    public string ShippedBy { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string Lines { get; set; } = "[]"; // JSONB
}
