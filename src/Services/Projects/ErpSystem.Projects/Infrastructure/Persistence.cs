using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Projects.Infrastructure;

#region Event Store DbContext

public class ProjectsEventStoreDbContext(DbContextOptions<ProjectsEventStoreDbContext> options) : DbContext(options)
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

public class ProjectsReadDbContext(DbContextOptions<ProjectsReadDbContext> options) : DbContext(options)
{
    public DbSet<ProjectReadModel> Projects { get; set; } = null!;
    public DbSet<TaskReadModel> Tasks { get; set; } = null!;
    public DbSet<TimesheetReadModel> Timesheets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Milestones).HasColumnType("jsonb");
            b.Property(x => x.TeamMembers).HasColumnType("jsonb");
            b.HasIndex(x => x.ProjectNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ProjectManagerId);
        });

        modelBuilder.Entity<TaskReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ProjectId);
            b.HasIndex(x => x.AssigneeId);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<TimesheetReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Entries).HasColumnType("jsonb");
            b.HasIndex(x => x.ProjectId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Status);
        });
    }
}

#endregion

#region Read Models

public class ProjectReadModel
{
    public Guid Id { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Internal";
    public string Status { get; set; } = "Planning";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PlannedBudget { get; set; }
    public decimal ActualCost { get; set; }
    public string Currency { get; set; } = "CNY";
    public string? CustomerId { get; set; }
    public string ProjectManagerId { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal ProgressPercent { get; set; }
    public string Milestones { get; set; } = "[]"; // JSONB
    public string TeamMembers { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
}

public class TaskReadModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public string? AssigneeId { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public int ProgressPercent { get; set; }
    public Guid? ParentTaskId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TimesheetReadModel
{
    public Guid Id { get; set; }
    public string TimesheetNumber { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal TotalHours { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string Entries { get; set; } = "[]"; // JSONB
}

#endregion
