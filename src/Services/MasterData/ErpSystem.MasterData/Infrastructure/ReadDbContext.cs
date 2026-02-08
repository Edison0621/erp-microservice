using Microsoft.EntityFrameworkCore;

namespace ErpSystem.MasterData.Infrastructure;

public class MasterDataReadDbContext : DbContext
{
    public DbSet<MaterialReadModel> Materials { get; set; }
    public DbSet<CategoryReadModel> Categories { get; set; }
    public DbSet<SupplierReadModel> Suppliers { get; set; }
    public DbSet<CustomerReadModel> Customers { get; set; }
    public DbSet<WarehouseReadModel> Warehouses { get; set; }
    public DbSet<LocationReadModel> Locations { get; set; }
    public DbSet<BOMReadModel> BOMs { get; set; }

    public MasterDataReadDbContext(DbContextOptions<MasterDataReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialReadModel>(b => 
        {
            b.HasKey(m => m.MaterialId);
            b.Property(m => m.Attributes).HasColumnType("jsonb");
            b.Property(m => m.CostDetail).HasColumnType("jsonb");
        });

        modelBuilder.Entity<CategoryReadModel>().HasKey(c => c.CategoryId);
        
        modelBuilder.Entity<SupplierReadModel>(b =>
        {
            b.HasKey(s => s.SupplierId);
            b.Property(s => s.Contacts).HasColumnType("jsonb");
            b.Property(s => s.BankAccounts).HasColumnType("jsonb");
        });

        modelBuilder.Entity<CustomerReadModel>(b =>
        {
            b.HasKey(c => c.CustomerId);
            b.Property(c => c.Addresses).HasColumnType("jsonb");
        });

        modelBuilder.Entity<WarehouseReadModel>().HasKey(w => w.WarehouseId);
        modelBuilder.Entity<LocationReadModel>().HasKey(l => l.LocationId);
        modelBuilder.Entity<BOMReadModel>(b =>
        {
            b.HasKey(bom => bom.BOMId);
            b.Property(bom => bom.Components).HasColumnType("jsonb");
        });
    }
}

// --- Read Models ---

public class MaterialReadModel
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal TotalCost { get; set; }
    public string CostDetail { get; set; } = "{}"; // JSON
    public string Attributes { get; set; } = "[]"; // JSON
    public bool IsActive { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
}

public class CategoryReadModel
{
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
}

public class SupplierReadModel
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }
    public string Contacts { get; set; } = "[]";
    public string BankAccounts { get; set; } = "[]";
}

public class CustomerReadModel
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public string Addresses { get; set; } = "[]";
}

public class WarehouseReadModel
{
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class LocationReadModel
{
    public Guid LocationId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class BOMReadModel
{
    public Guid BOMId { get; set; }
    public Guid ParentMaterialId { get; set; }
    public string BOMName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string Components { get; set; } = "[]"; // JSON array of BOMComponent
}
