using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.CRM.Domain;

#region Enums

/// <summary>
/// Campaign status throughout its lifecycle
/// </summary>
public enum CampaignStatus
{
    Draft = 0,
    Scheduled = 1,
    Active = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5
}

/// <summary>
/// Campaign types
/// </summary>
public enum CampaignType
{
    Email = 0,
    SocialMedia = 1,
    TradeShow = 2,
    Webinar = 3,
    Advertisement = 4,
    DirectMail = 5,
    Telemarketing = 6,
    Referral = 7,
    Other = 8
}

#endregion

#region Domain Events

public record CampaignCreatedEvent(
    Guid CampaignId,
    string CampaignNumber,
    string Name,
    CampaignType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Budget,
    string Currency,
    string? TargetAudience,
    string? Description,
    string CreatedByUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CampaignStatusChangedEvent(
    Guid CampaignId,
    CampaignStatus OldStatus,
    CampaignStatus NewStatus
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CampaignBudgetUpdatedEvent(
    Guid CampaignId,
    decimal OldBudget,
    decimal NewBudget,
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record LeadAssociatedToCampaignEvent(
    Guid CampaignId,
    Guid LeadId,
    string LeadNumber,
    DateTime AssociatedAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CampaignExpenseRecordedEvent(
    Guid CampaignId,
    Guid ExpenseId,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string RecordedByUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CampaignMetricsUpdatedEvent(
    Guid CampaignId,
    int TotalLeads,
    int ConvertedLeads,
    decimal TotalRevenue
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Supporting Types

public record CampaignLead(
    Guid LeadId,
    string LeadNumber,
    DateTime AssociatedAt
);

public record CampaignExpense(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string RecordedByUserId
);

#endregion

#region Campaign Aggregate

/// <summary>
/// Campaign aggregate root - represents a marketing campaign
/// </summary>
public class Campaign : AggregateRoot<Guid>
{
    public string CampaignNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CampaignType Type { get; private set; }
    public CampaignStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Budget { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public string? TargetAudience { get; private set; }
    public string? Description { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    
    // Metrics
    public int TotalLeads { get; private set; }
    public int ConvertedLeads { get; private set; }
    public decimal TotalRevenue { get; private set; }
    
    public List<CampaignLead> Leads { get; private set; } = [];
    public List<CampaignExpense> Expenses { get; private set; } = [];

    /// <summary>
    /// Total expenses for the campaign
    /// </summary>
    public decimal TotalExpenses => this.Expenses.Sum(e => e.Amount);

    /// <summary>
    /// Budget utilization percentage
    /// </summary>
    public decimal BudgetUtilization => this.Budget > 0 ? this.TotalExpenses / this.Budget * 100 : 0;

    /// <summary>
    /// Return on Investment (ROI)
    /// </summary>
    public decimal Roi => this.TotalExpenses > 0 ? (this.TotalRevenue - this.TotalExpenses) / this.TotalExpenses * 100 : 0;

    /// <summary>
    /// Cost per Lead
    /// </summary>
    public decimal CostPerLead => this.TotalLeads > 0 ? this.TotalExpenses / this.TotalLeads : 0;

    /// <summary>
    /// Conversion Rate
    /// </summary>
    public decimal ConversionRate => this.TotalLeads > 0 ? (decimal)this.ConvertedLeads / this.TotalLeads * 100 : 0;

    public static Campaign Create(
        Guid id,
        string campaignNumber,
        string name,
        CampaignType type,
        DateTime startDate,
        DateTime endDate,
        decimal budget,
        string currency,
        string createdByUserId,
        string? targetAudience = null,
        string? description = null)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");

        Campaign campaign = new Campaign();
        campaign.ApplyChange(new CampaignCreatedEvent(
            id, campaignNumber, name, type, startDate, endDate,
            budget, currency, targetAudience, description, createdByUserId));
        return campaign;
    }

    public void Start()
    {
        if (this.Status != CampaignStatus.Draft && this.Status != CampaignStatus.Scheduled)
            throw new InvalidOperationException("Only draft or scheduled campaigns can be started");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Active));
    }

    public void Pause()
    {
        if (this.Status != CampaignStatus.Active)
            throw new InvalidOperationException("Only active campaigns can be paused");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Paused));
    }

    public void Resume()
    {
        if (this.Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only paused campaigns can be resumed");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Active));
    }

    public void Complete()
    {
        if (this.Status != CampaignStatus.Active && this.Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only active or paused campaigns can be completed");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Completed));
    }

    public void Cancel()
    {
        if (this.Status == CampaignStatus.Completed || this.Status == CampaignStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled campaign");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Cancelled));
    }

    public void Schedule()
    {
        if (this.Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Only draft campaigns can be scheduled");

        this.ApplyChange(new CampaignStatusChangedEvent(this.Id, this.Status, CampaignStatus.Scheduled));
    }

    public void UpdateBudget(decimal newBudget, string? reason = null)
    {
        if (this.Status == CampaignStatus.Completed || this.Status == CampaignStatus.Cancelled)
            throw new InvalidOperationException("Cannot update budget of a completed or cancelled campaign");

        if (newBudget < 0)
            throw new ArgumentException("Budget cannot be negative");

        this.ApplyChange(new CampaignBudgetUpdatedEvent(this.Id, this.Budget, newBudget, reason));
    }

    public void AssociateLead(Guid leadId, string leadNumber)
    {
        if (this.Leads.Any(l => l.LeadId == leadId))
            throw new InvalidOperationException("Lead is already associated with this campaign");

        this.ApplyChange(new LeadAssociatedToCampaignEvent(this.Id, leadId, leadNumber, DateTime.UtcNow));
    }

    public void RecordExpense(string description, decimal amount, DateTime expenseDate, string recordedByUserId)
    {
        if (this.Status == CampaignStatus.Completed || this.Status == CampaignStatus.Cancelled)
            throw new InvalidOperationException("Cannot record expense for a completed or cancelled campaign");

        if (amount <= 0)
            throw new ArgumentException("Expense amount must be positive");

        Guid expenseId = Guid.NewGuid();
        this.ApplyChange(new CampaignExpenseRecordedEvent(this.Id, expenseId, description, amount, expenseDate, recordedByUserId));
    }

    public void UpdateMetrics(int totalLeads, int convertedLeads, decimal totalRevenue)
    {
        this.ApplyChange(new CampaignMetricsUpdatedEvent(this.Id, totalLeads, convertedLeads, totalRevenue));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CampaignCreatedEvent e:
                this.Id = e.CampaignId;
                this.CampaignNumber = e.CampaignNumber;
                this.Name = e.Name;
                this.Type = e.Type;
                this.Status = CampaignStatus.Draft;
                this.StartDate = e.StartDate;
                this.EndDate = e.EndDate;
                this.Budget = e.Budget;
                this.Currency = e.Currency;
                this.TargetAudience = e.TargetAudience;
                this.Description = e.Description;
                this.CreatedByUserId = e.CreatedByUserId;
                break;

            case CampaignStatusChangedEvent e:
                this.Status = e.NewStatus;
                break;

            case CampaignBudgetUpdatedEvent e:
                this.Budget = e.NewBudget;
                break;

            case LeadAssociatedToCampaignEvent e:
                this.Leads.Add(new CampaignLead(e.LeadId, e.LeadNumber, e.AssociatedAt));
                this.TotalLeads = this.Leads.Count;
                break;

            case CampaignExpenseRecordedEvent e:
                this.Expenses.Add(new CampaignExpense(
                    e.ExpenseId, e.Description, e.Amount, e.ExpenseDate, e.RecordedByUserId));
                break;

            case CampaignMetricsUpdatedEvent e:
                this.TotalLeads = e.TotalLeads;
                this.ConvertedLeads = e.ConvertedLeads;
                this.TotalRevenue = e.TotalRevenue;
                break;
        }
    }
}

#endregion
