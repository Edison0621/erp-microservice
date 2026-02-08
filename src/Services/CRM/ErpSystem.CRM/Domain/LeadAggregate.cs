using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.CRM.Domain;

#region Enums

/// <summary>
/// Lead status throughout its lifecycle
/// </summary>
public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Unqualified = 3,
    Converted = 4,
    Lost = 5
}

/// <summary>
/// Source channels for leads
/// </summary>
public enum LeadSource
{
    Website = 0,
    Referral = 1,
    Advertisement = 2,
    SocialMedia = 3,
    TradeShow = 4,
    ColdCall = 5,
    EmailCampaign = 6,
    Partner = 7,
    Other = 8
}

#endregion

#region Value Objects

/// <summary>
/// Contact information for a lead
/// </summary>
public record ContactInfo(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Mobile
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Company information associated with a lead
/// </summary>
public record CompanyInfo(
    string CompanyName,
    string Industry,
    string CompanySize,
    string Website,
    string Address
);

#endregion

#region Domain Events

public record LeadCreatedEvent(
    Guid LeadId,
    string LeadNumber,
    ContactInfo Contact,
    CompanyInfo? Company,
    LeadSource Source,
    string? SourceDetails,
    string? AssignedToUserId,
    string? Notes
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record LeadStatusChangedEvent(
    Guid LeadId,
    LeadStatus OldStatus,
    LeadStatus NewStatus,
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record LeadQualifiedEvent(
    Guid LeadId,
    int Score,
    string QualificationNotes
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record LeadConvertedToOpportunityEvent(
    Guid LeadId,
    Guid OpportunityId,
    string OpportunityName,
    decimal EstimatedValue
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record LeadAssignedEvent(
    Guid LeadId,
    string? OldUserId,
    string NewUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CommunicationLoggedEvent(
    Guid LeadId,
    Guid CommunicationId,
    CommunicationType Type,
    string Subject,
    string Content,
    DateTime CommunicationDate,
    string LoggedByUserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Communication Types

public enum CommunicationType
{
    Phone = 0,
    Email = 1,
    Meeting = 2,
    Visit = 3,
    WebConference = 4,
    Other = 5
}

public record CommunicationRecord(
    Guid Id,
    CommunicationType Type,
    string Subject,
    string Content,
    DateTime CommunicationDate,
    string LoggedByUserId
);

#endregion

#region Lead Aggregate

/// <summary>
/// Lead aggregate root - represents a potential customer
/// </summary>
public class Lead : AggregateRoot<Guid>
{
    public string LeadNumber { get; private set; } = string.Empty;
    public ContactInfo Contact { get; private set; } = null!;
    public CompanyInfo? Company { get; private set; }
    public LeadStatus Status { get; private set; }
    public LeadSource Source { get; private set; }
    public string? SourceDetails { get; private set; }
    public string? AssignedToUserId { get; private set; }
    public string? Notes { get; private set; }
    public int Score { get; private set; }
    public Guid? ConvertedOpportunityId { get; private set; }
    public List<CommunicationRecord> Communications { get; private set; } = new();

    public static Lead Create(
        Guid id,
        string leadNumber,
        ContactInfo contact,
        CompanyInfo? company,
        LeadSource source,
        string? sourceDetails = null,
        string? assignedToUserId = null,
        string? notes = null)
    {
        var lead = new Lead();
        lead.ApplyChange(new LeadCreatedEvent(
            id, leadNumber, contact, company, source, sourceDetails, assignedToUserId, notes));
        return lead;
    }

    public void ChangeStatus(LeadStatus newStatus, string? reason = null)
    {
        if (Status == LeadStatus.Converted)
            throw new InvalidOperationException("Cannot change status of a converted lead");

        if (Status == newStatus)
            return;

        ApplyChange(new LeadStatusChangedEvent(Id, Status, newStatus, reason));
    }

    public void Qualify(int score, string qualificationNotes)
    {
        if (Status == LeadStatus.Converted || Status == LeadStatus.Lost)
            throw new InvalidOperationException("Cannot qualify a converted or lost lead");

        if (score < 0 || score > 100)
            throw new ArgumentException("Score must be between 0 and 100");

        ApplyChange(new LeadQualifiedEvent(Id, score, qualificationNotes));
    }

    public Guid ConvertToOpportunity(string opportunityName, decimal estimatedValue)
    {
        if (Status == LeadStatus.Converted)
            throw new InvalidOperationException("Lead is already converted");

        if (Status == LeadStatus.Lost || Status == LeadStatus.Unqualified)
            throw new InvalidOperationException("Cannot convert a lost or unqualified lead");

        var opportunityId = Guid.NewGuid();
        ApplyChange(new LeadConvertedToOpportunityEvent(Id, opportunityId, opportunityName, estimatedValue));
        return opportunityId;
    }

    public void AssignTo(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty");

        ApplyChange(new LeadAssignedEvent(Id, AssignedToUserId, userId));
    }

    public void LogCommunication(
        CommunicationType type,
        string subject,
        string content,
        DateTime communicationDate,
        string loggedByUserId)
    {
        var communicationId = Guid.NewGuid();
        ApplyChange(new CommunicationLoggedEvent(
            Id, communicationId, type, subject, content, communicationDate, loggedByUserId));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case LeadCreatedEvent e:
                Id = e.LeadId;
                LeadNumber = e.LeadNumber;
                Contact = e.Contact;
                Company = e.Company;
                Source = e.Source;
                SourceDetails = e.SourceDetails;
                AssignedToUserId = e.AssignedToUserId;
                Notes = e.Notes;
                Status = LeadStatus.New;
                Score = 0;
                break;

            case LeadStatusChangedEvent e:
                Status = e.NewStatus;
                break;

            case LeadQualifiedEvent e:
                Score = e.Score;
                if (e.Score >= 70)
                    Status = LeadStatus.Qualified;
                break;

            case LeadConvertedToOpportunityEvent e:
                Status = LeadStatus.Converted;
                ConvertedOpportunityId = e.OpportunityId;
                break;

            case LeadAssignedEvent e:
                AssignedToUserId = e.NewUserId;
                break;

            case CommunicationLoggedEvent e:
                Communications.Add(new CommunicationRecord(
                    e.CommunicationId,
                    e.Type,
                    e.Subject,
                    e.Content,
                    e.CommunicationDate,
                    e.LoggedByUserId));
                if (Status == LeadStatus.New)
                    Status = LeadStatus.Contacted;
                break;
        }
    }
}

#endregion
