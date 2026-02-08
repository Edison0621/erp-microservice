using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Inventory.Infrastructure;

public class InventoryEventStoreDbContext : DbContext
{
    public DbSet<EventStream> Events { get; set; } = null!;

    public InventoryEventStoreDbContext(DbContextOptions<InventoryEventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class InventoryReadDbContext : DbContext
{
    public DbSet<InventoryItemReadModel> InventoryItems { get; set; } = null!;
    public DbSet<StockTransactionReadModel> StockTransactions { get; set; } = null!;
    public DbSet<StockReservationReadModel> StockReservations { get; set; } = null!;

    public InventoryReadDbContext(DbContextOptions<InventoryReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItemReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<StockTransactionReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<StockReservationReadModel>().HasKey(x => x.Id);
        
        // Indexing for faster queries - Unique per Material in a specific Bin
        modelBuilder.Entity<InventoryItemReadModel>()
            .HasIndex(x => new { x.WarehouseId, x.BinId, x.MaterialId })
            .IsUnique();
    }
}

public class InventoryItemReadModel
{
    public Guid Id { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public string BinId { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal OnHandQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime LastMovementAt { get; set; }
}

public class StockTransactionReadModel
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public decimal QuantityChange { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
}

public class StockReservationReadModel
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsReleased { get; set; }
}
