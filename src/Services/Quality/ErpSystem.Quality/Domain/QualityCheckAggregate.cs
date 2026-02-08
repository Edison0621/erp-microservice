using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Quality.Domain;

/// <summary>
/// Quality Check - An individual check instance linked to a Quality Point
/// </summary>
public class QualityCheck : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid QualityPointId { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public string SourceType { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public QualityCheckStatus Status { get; private set; }
    public string Result { get; private set; } = string.Empty;

    public static QualityCheck Create(
        Guid id,
        string tenantId,
        Guid qualityPointId,
        string sourceId,
        string sourceType,
        string materialId)
    {
        var qc = new QualityCheck();
        qc.ApplyChange(new QualityCheckCreatedEvent(
            id,
            tenantId,
            qualityPointId,
            sourceId,
            sourceType,
            materialId,
            DateTime.UtcNow));
        return qc;
    }

    public void Pass(string? note, string performedBy)
    {
        if (Status != QualityCheckStatus.Pending)
            throw new InvalidOperationException("Only pending checks can be passed");

        ApplyChange(new QualityCheckPassedEvent(Id, note, performedBy, DateTime.UtcNow));
    }

    public void Fail(string reason, string performedBy)
    {
        if (Status != QualityCheckStatus.Pending)
            throw new InvalidOperationException("Only pending checks can be failed");

        ApplyChange(new QualityCheckFailedEvent(Id, reason, performedBy, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case QualityCheckCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                QualityPointId = e.QualityPointId;
                SourceId = e.SourceId;
                SourceType = e.SourceType;
                MaterialId = e.MaterialId;
                Status = QualityCheckStatus.Pending;
                break;
            case QualityCheckPassedEvent:
                Status = QualityCheckStatus.Passed;
                break;
            case QualityCheckFailedEvent:
                Status = QualityCheckStatus.Failed;
                break;
        }
    }
}

public enum QualityCheckStatus
{
    Pending = 1,
    Passed = 2,
    Failed = 3
}

public record QualityCheckCreatedEvent(
    Guid AggregateId,
    string TenantId,
    Guid QualityPointId,
    string SourceId,
    string SourceType,
    string MaterialId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record QualityCheckPassedEvent(
    Guid AggregateId,
    string? Note,
    string PerformedBy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record QualityCheckFailedEvent(
    Guid AggregateId,
    string Reason,
    string PerformedBy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
