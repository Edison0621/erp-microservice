using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.CRM.Infrastructure;

#region Event Store DbContext

public class CrmEventStoreDbContext(DbContextOptions<CrmEventStoreDbContext> options) : DbContext(options)
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

public class CrmReadDbContext(DbContextOptions<CrmReadDbContext> options) : DbContext(options)
{
    public DbSet<LeadReadModel> Leads { get; set; } = null!;
    public DbSet<OpportunityReadModel> Opportunities { get; set; } = null!;
    public DbSet<CampaignReadModel> Campaigns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeadReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Contact).HasColumnType("jsonb");
            b.Property(x => x.Company).HasColumnType("jsonb");
            b.Property(x => x.Communications).HasColumnType("jsonb");
            b.HasIndex(x => x.LeadNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AssignedToUserId);
        });

        modelBuilder.Entity<OpportunityReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Competitors).HasColumnType("jsonb");
            b.Property(x => x.Activities).HasColumnType("jsonb");
            b.HasIndex(x => x.OpportunityNumber).IsUnique();
            b.HasIndex(x => x.Stage);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<CampaignReadModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.AssociatedLeads).HasColumnType("jsonb");
            b.Property(x => x.Expenses).HasColumnType("jsonb");
            b.HasIndex(x => x.CampaignNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Type);
        });
    }
}

#endregion

#region Read Models

public class LeadReadModel
{
    public Guid Id { get; set; }
    public string LeadNumber { get; set; } = string.Empty;
    public string Contact { get; set; } = "{}"; // JSONB - ContactInfo
    public string? Company { get; set; } = "{}"; // JSONB - CompanyInfo
    public string Status { get; set; } = "New";
    public string Source { get; set; } = "Website";
    public string? SourceDetails { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public int Score { get; set; }
    public Guid? ConvertedOpportunityId { get; set; }
    public string Communications { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
    public DateTime? LastContactedAt { get; set; }
}

public class OpportunityReadModel
{
    public Guid Id { get; set; }
    public string OpportunityNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? LeadId { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal WeightedValue { get; set; }
    public string Currency { get; set; } = "CNY";
    public DateTime ExpectedCloseDate { get; set; }
    public string Stage { get; set; } = "Prospecting";
    public string Priority { get; set; } = "Medium";
    public int WinProbability { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? Description { get; set; }
    public string? WinReason { get; set; }
    public string? LossReason { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string Competitors { get; set; } = "[]"; // JSONB
    public string Activities { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class CampaignReadModel
{
    public Guid Id { get; set; }
    public string CampaignNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Email";
    public string Status { get; set; } = "Draft";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public decimal TotalExpenses { get; set; }
    public string Currency { get; set; } = "CNY";
    public string? TargetAudience { get; set; }
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    
    // Metrics
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal Roi { get; set; }
    public decimal CostPerLead { get; set; }
    public decimal ConversionRate { get; set; }
    
    public string AssociatedLeads { get; set; } = "[]"; // JSONB
    public string Expenses { get; set; } = "[]"; // JSONB
    public DateTime CreatedAt { get; set; }
}

#endregion
