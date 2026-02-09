using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Procurement.Infrastructure;

public class ProcurementEventStoreDbContext(DbContextOptions<ProcurementEventStoreDbContext> options) : DbContext(options)
{
    public DbSet<EventStream> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class ProcurementReadDbContext(DbContextOptions<ProcurementReadDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseOrderReadModel> PurchaseOrders { get; set; } = null!;
    public DbSet<GoodsReceiptReadModel> GoodsReceipts { get; set; } = null!;
    public DbSet<SupplierPriceHistory> PriceHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderReadModel>().Property(x => x.Lines).HasColumnType("jsonb");
        modelBuilder.Entity<GoodsReceiptReadModel>().Property(x => x.Lines).HasColumnType("jsonb");
    }
}

public class PurchaseOrderReadModel
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "CNY";
    public decimal TotalAmount { get; set; }
    public string Lines { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
}

public class GoodsReceiptReadModel
{
    public Guid Id { get; set; }
    public string GrNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string Lines { get; set; } = "[]"; // JSONB
}

public class SupplierPriceHistory
{
    public Guid Id { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "CNY";
    public DateTime EffectiveDate { get; set; }
}
