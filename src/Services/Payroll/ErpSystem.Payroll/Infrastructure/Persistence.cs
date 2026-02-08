using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Payroll.Infrastructure;

#region Event Store DbContext

public class PayrollEventStoreDbContext : DbContext
{
    public DbSet<EventStream> Events { get; set; } = null!;

    public PayrollEventStoreDbContext(DbContextOptions<PayrollEventStoreDbContext> options) : base(options) { }

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

public class PayrollReadDbContext : DbContext
{
    public DbSet<SalaryStructureReadModel> SalaryStructures { get; set; } = null!;
    public DbSet<PayrollRunReadModel> PayrollRuns { get; set; } = null!;
    public DbSet<PayslipReadModel> Payslips { get; set; } = null!;

    public PayrollReadDbContext(DbContextOptions<PayrollReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalaryStructureReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Components).HasColumnType("jsonb");
            b.Property(x => x.Deductions).HasColumnType("jsonb");
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<PayrollRunReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RunNumber).IsUnique();
            b.HasIndex(x => new { x.Year, x.Month });
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<PayslipReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Lines).HasColumnType("jsonb");
            b.HasIndex(x => x.PayrollRunId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.Status);
        });
    }
}

#endregion

#region Read Models

public class SalaryStructureReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "CNY";
    public bool IsActive { get; set; } = true;
    public decimal TotalEarnings { get; set; }
    public int ComponentCount { get; set; }
    public int DeductionCount { get; set; }
    public string Components { get; set; } = "[]"; // JSONB
    public string Deductions { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
}

public class PayrollRunReadModel
{
    public Guid Id { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public int EmployeeCount { get; set; }
    public int PaidCount { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
}

public class PayslipReadModel
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionRef { get; set; }
    public string Lines { get; set; } = "[]"; // JSONB
}

#endregion
