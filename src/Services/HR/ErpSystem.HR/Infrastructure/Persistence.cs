using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.HR.Infrastructure;

public class HrEventStoreDbContext(DbContextOptions<HrEventStoreDbContext> options) : DbContext(options)
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

public class HrReadDbContext(DbContextOptions<HrReadDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeReadModel> Employees { get; set; } = null!;
    public DbSet<EmployeeEventReadModel> EmployeeEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<EmployeeEventReadModel>().HasKey(x => x.Id);
    }
}

public class EmployeeReadModel
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string IdType { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public string ManagerEmployeeId { get; set; } = string.Empty;
    public string CostCenterId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EmployeeEventReadModel
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FromDepartmentId { get; set; } = string.Empty;
    public string ToDepartmentId { get; set; } = string.Empty;
    public string FromPositionId { get; set; } = string.Empty;
    public string ToPositionId { get; set; } = string.Empty;
}
