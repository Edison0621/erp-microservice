using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Assets.Infrastructure;

#region Event Store DbContext

public class AssetsEventStoreDbContext(DbContextOptions<AssetsEventStoreDbContext> options) : DbContext(options)
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

#endregion

#region Read DbContext

public class AssetsReadDbContext(DbContextOptions<AssetsReadDbContext> options) : DbContext(options)
{
    public DbSet<AssetReadModel> Assets { get; set; } = null!;
    public DbSet<MaintenanceReadModel> MaintenanceRecords { get; set; } = null!;
    public DbSet<DepreciationReadModel> DepreciationRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AssetNumber).IsUnique();
            b.HasIndex(x => x.Type);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.LocationId);
        });

        modelBuilder.Entity<MaintenanceReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AssetId);
            b.HasIndex(x => x.MaintenanceDate);
        });

        modelBuilder.Entity<DepreciationReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AssetId);
            b.HasIndex(x => new { x.Year, x.Month });
        });
    }
}

#endregion

#region Read Models

public class AssetReadModel
{
    public Guid Id { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "FixedAsset";
    public string Status { get; set; } = "Draft";
    
    // Financial
    public decimal AcquisitionCost { get; set; }
    public DateTime AcquisitionDate { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal SalvageValue { get; set; }
    public decimal BookValue { get; set; }
    
    // Depreciation
    public string DepreciationMethod { get; set; } = "StraightLine";
    public int UsefulLifeMonths { get; set; }
    public decimal MonthlyDepreciation { get; set; }
    
    // Location
    public string LocationId { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? AssignedToUserId { get; set; }
    
    // Calculated
    public decimal TotalMaintenanceCost { get; set; }
    public int MaintenanceCount { get; set; }
    public int TransferCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? DisposedAt { get; set; }
}

public class MaintenanceReadModel
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Type { get; set; } = "Preventive";
    public string Description { get; set; } = string.Empty;
    public DateTime MaintenanceDate { get; set; }
    public decimal Cost { get; set; }
    public string? PerformedBy { get; set; }
}

public class DepreciationReadModel
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
}

#endregion
