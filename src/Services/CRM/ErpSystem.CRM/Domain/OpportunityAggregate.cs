using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.CRM.Domain;

#region Enums

/// <summary>
/// Sales pipeline stages for opportunities
/// </summary>
public enum OpportunityStage
{
    Prospecting = 0,        // 10% probability
    Qualification = 1,      // 20% probability
    NeedsAnalysis = 2,      // 40% probability
    ValueProposition = 3,   // 60% probability
    Negotiation = 4,        // 80% probability
    ClosedWon = 5,          // 100% probability
    ClosedLost = 6          // 0% probability
}

/// <summary>
/// Priority levels for opportunities
/// </summary>
public enum OpportunityPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

#endregion

#region Domain Events

public record OpportunityCreatedEvent(
    Guid OpportunityId,
    string OpportunityNumber,
    string Name,
    Guid? LeadId,
    string? CustomerId,
    string? CustomerName,
    decimal EstimatedValue,
    string Currency,
    DateTime ExpectedCloseDate,
    OpportunityPriority Priority,
    string? AssignedToUserId,
    string? Description
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityStageChangedEvent(
    Guid OpportunityId,
    OpportunityStage OldStage,
    OpportunityStage NewStage,
    string? Notes,
    int WinProbability
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityValueUpdatedEvent(
    Guid OpportunityId,
    decimal OldValue,
    decimal NewValue,
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityWonEvent(
    Guid OpportunityId,
    decimal FinalValue,
    string? WinReason,
    Guid? SalesOrderId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityLostEvent(
    Guid OpportunityId,
    string LossReason,
    string? CompetitorId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityAssignedEvent(
    Guid OpportunityId,
    string? OldUserId,
    string NewUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityCompetitorAddedEvent(
    Guid OpportunityId,
    string CompetitorId,
    string CompetitorName,
    string? Strengths,
    string? Weaknesses
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record OpportunityActivityLoggedEvent(
    Guid OpportunityId,
    Guid ActivityId,
    string ActivityType,
    string Subject,
    string Description,
    DateTime ActivityDate,
    string LoggedByUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Supporting Types

public record CompetitorInfo(
    string CompetitorId,
    string CompetitorName,
    string? Strengths,
    string? Weaknesses
);

public record ActivityRecord(
    Guid Id,
    string ActivityType,
    string Subject,
    string Description,
    DateTime ActivityDate,
    string LoggedByUserId
);

#endregion

#region Opportunity Aggregate

/// <summary>
/// Opportunity aggregate root - represents a sales opportunity in the pipeline
/// </summary>
public class Opportunity : AggregateRoot<Guid>
{
    public string OpportunityNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? LeadId { get; private set; }
    public string? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public decimal EstimatedValue { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public DateTime ExpectedCloseDate { get; private set; }
    public OpportunityStage Stage { get; private set; }
    public OpportunityPriority Priority { get; private set; }
    public int WinProbability { get; private set; }
    public string? AssignedToUserId { get; private set; }
    public string? Description { get; private set; }
    public string? WinReason { get; private set; }
    public string? LossReason { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public List<CompetitorInfo> Competitors { get; private set; } = [];
    public List<ActivityRecord> Activities { get; private set; } = [];

    /// <summary>
    /// Weighted value based on win probability
    /// </summary>
    public decimal WeightedValue => this.EstimatedValue * this.WinProbability / 100m;

    public static Opportunity Create(
        Guid id,
        string opportunityNumber,
        string name,
        Guid? leadId,
        string? customerId,
        string? customerName,
        decimal estimatedValue,
        string currency,
        DateTime expectedCloseDate,
        OpportunityPriority priority,
        string? assignedToUserId = null,
        string? description = null)
    {
        Opportunity opportunity = new();
        opportunity.ApplyChange(new OpportunityCreatedEvent(
            id, opportunityNumber, name, leadId, customerId, customerName,
            estimatedValue, currency, expectedCloseDate, priority,
            assignedToUserId, description));
        return opportunity;
    }

    public void AdvanceStage(OpportunityStage newStage, string? notes = null)
    {
        if (this.Stage == OpportunityStage.ClosedWon || this.Stage == OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Cannot change stage of a closed opportunity");

        if (newStage == OpportunityStage.ClosedWon || newStage == OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Use MarkAsWon or MarkAsLost methods for closing");

        int probability = GetStageProbability(newStage);
        this.ApplyChange(new OpportunityStageChangedEvent(this.Id, this.Stage, newStage, notes, probability));
    }

    public void UpdateValue(decimal newValue, string? reason = null)
    {
        if (this.Stage == OpportunityStage.ClosedWon || this.Stage == OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Cannot update value of a closed opportunity");

        if (newValue < 0)
            throw new ArgumentException("Value cannot be negative");

        this.ApplyChange(new OpportunityValueUpdatedEvent(this.Id, this.EstimatedValue, newValue, reason));
    }

    public void MarkAsWon(decimal? finalValue = null, string? winReason = null, Guid? salesOrderId = null)
    {
        if (this.Stage == OpportunityStage.ClosedWon || this.Stage == OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Opportunity is already closed");

        decimal value = finalValue ?? this.EstimatedValue;
        this.ApplyChange(new OpportunityWonEvent(this.Id, value, winReason, salesOrderId));
    }

    public void MarkAsLost(string lossReason, string? competitorId = null)
    {
        if (this.Stage == OpportunityStage.ClosedWon || this.Stage == OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Opportunity is already closed");

        if (string.IsNullOrWhiteSpace(lossReason))
            throw new ArgumentException("Loss reason is required");

        this.ApplyChange(new OpportunityLostEvent(this.Id, lossReason, competitorId));
    }

    public void AssignTo(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty");

        this.ApplyChange(new OpportunityAssignedEvent(this.Id, this.AssignedToUserId, userId));
    }

    public void AddCompetitor(string competitorId, string competitorName, string? strengths = null, string? weaknesses = null)
    {
        if (this.Competitors.Any(c => c.CompetitorId == competitorId))
            throw new InvalidOperationException("Competitor already added");

        this.ApplyChange(new OpportunityCompetitorAddedEvent(this.Id, competitorId, competitorName, strengths, weaknesses));
    }

    public void LogActivity(string activityType, string subject, string description, DateTime activityDate, string loggedByUserId)
    {
        Guid activityId = Guid.NewGuid();
        this.ApplyChange(new OpportunityActivityLoggedEvent(this.Id, activityId, activityType, subject, description, activityDate, loggedByUserId));
    }

    private static int GetStageProbability(OpportunityStage stage) => stage switch
    {
        OpportunityStage.Prospecting => 10,
        OpportunityStage.Qualification => 20,
        OpportunityStage.NeedsAnalysis => 40,
        OpportunityStage.ValueProposition => 60,
        OpportunityStage.Negotiation => 80,
        OpportunityStage.ClosedWon => 100,
        OpportunityStage.ClosedLost => 0,
        _ => 0
    };

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OpportunityCreatedEvent e:
                this.Id = e.OpportunityId;
                this.OpportunityNumber = e.OpportunityNumber;
                this.Name = e.Name;
                this.LeadId = e.LeadId;
                this.CustomerId = e.CustomerId;
                this.CustomerName = e.CustomerName;
                this.EstimatedValue = e.EstimatedValue;
                this.Currency = e.Currency;
                this.ExpectedCloseDate = e.ExpectedCloseDate;
                this.Priority = e.Priority;
                this.AssignedToUserId = e.AssignedToUserId;
                this.Description = e.Description;
                this.Stage = OpportunityStage.Prospecting;
                this.WinProbability = 10;
                break;

            case OpportunityStageChangedEvent e:
                this.Stage = e.NewStage;
                this.WinProbability = e.WinProbability;
                break;

            case OpportunityValueUpdatedEvent e:
                this.EstimatedValue = e.NewValue;
                break;

            case OpportunityWonEvent e:
                this.Stage = OpportunityStage.ClosedWon;
                this.WinProbability = 100;
                this.EstimatedValue = e.FinalValue;
                this.WinReason = e.WinReason;
                this.SalesOrderId = e.SalesOrderId;
                break;

            case OpportunityLostEvent e:
                this.Stage = OpportunityStage.ClosedLost;
                this.WinProbability = 0;
                this.LossReason = e.LossReason;
                break;

            case OpportunityAssignedEvent e:
                this.AssignedToUserId = e.NewUserId;
                break;

            case OpportunityCompetitorAddedEvent e:
                this.Competitors.Add(new CompetitorInfo(e.CompetitorId, e.CompetitorName, e.Strengths, e.Weaknesses));
                break;

            case OpportunityActivityLoggedEvent e:
                this.Activities.Add(new ActivityRecord(
                    e.ActivityId, e.ActivityType, e.Subject, e.Description, e.ActivityDate, e.LoggedByUserId));
                break;
        }
    }
}

#endregion
