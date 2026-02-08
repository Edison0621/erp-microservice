using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Quality.Domain;

/// <summary>
/// Quality Alert - Raised when a quality issue is detected
/// </summary>
public class QualityAlert : AggregateRoot<Guid>
{
    public string Description { get; private set; } = string.Empty;
    public QualityAlertPriority Priority { get; private set; }
    public QualityAlertStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }

    public static QualityAlert Create(
        Guid id,
        string tenantId,
        string description,
        string materialId,
        Guid sourceId,
        QualityAlertPriority priority)
    {
        var qa = new QualityAlert();
        qa.ApplyChange(new QualityAlertCreatedEvent(
            id,
            tenantId,
            description,
            materialId,
            sourceId,
            priority,
            DateTime.UtcNow));
        return qa;
    }

    public void Assign(string assignedTo)
    {
        ApplyChange(new QualityAlertAssignedEvent(Id, assignedTo, DateTime.UtcNow));
    }

    public void Resolve(string resolution)
    {
        ApplyChange(new QualityAlertResolvedEvent(Id, resolution, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case QualityAlertCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                Description = e.Description;
                MaterialId = e.MaterialId;
                SourceId = e.SourceId;
                Priority = e.Priority;
                Status = QualityAlertStatus.New;
                break;
            case QualityAlertAssignedEvent e:
                AssignedTo = e.AssignedTo;
                Status = QualityAlertStatus.InProgress;
                break;
            case QualityAlertResolvedEvent:
                Status = QualityAlertStatus.Resolved;
                break;
        }
    }
}

public enum QualityAlertPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum QualityAlertStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public record QualityAlertCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string Description,
    string MaterialId,
    Guid SourceId,
    QualityAlertPriority Priority,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record QualityAlertAssignedEvent(
    Guid AggregateId,
    string AssignedTo,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record QualityAlertResolvedEvent(
    Guid AggregateId,
    string Resolution,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
