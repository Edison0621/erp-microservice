using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Production.Infrastructure;

public class ProductionEventStoreDbContext : DbContext
{
    public DbSet<EventStream> Events { get; set; } = null!;

    public ProductionEventStoreDbContext(DbContextOptions<ProductionEventStoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class ProductionReadDbContext : DbContext
{
    public DbSet<ProductionOrderReadModel> ProductionOrders { get; set; } = null!;
    public DbSet<ProductionReportReadModel> ProductionReports { get; set; } = null!;
    public DbSet<MaterialConsumptionReadModel> MaterialConsumptions { get; set; } = null!;

    public ProductionReadDbContext(DbContextOptions<ProductionReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductionOrderReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ProductionReportReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<MaterialConsumptionReadModel>().HasKey(x => x.Id);
    }
}

public class ProductionOrderReadModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public decimal ReportedQuantity { get; set; }
    public decimal ScrappedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
}

public class ProductionReportReadModel
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public DateTime ReportedAt { get; set; }
    public decimal GoodQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
}

public class MaterialConsumptionReadModel
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public string MaterialId { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime ConsumedAt { get; set; }
    public string ConsumedBy { get; set; } = string.Empty;
}
