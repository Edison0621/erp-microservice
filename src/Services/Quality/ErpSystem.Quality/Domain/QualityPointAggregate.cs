using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Quality.Domain;

/// <summary>
/// Quality Control Point - Defines a requirement for a specific operation or material
/// </summary>
public class QualityPoint : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public string OperationType { get; private set; } = string.Empty; // e.g., "RECEIPT", "PRODUCTION_START", "PACKING"
    public QualityCheckType CheckType { get; private set; }
    public string Instructions { get; private set; } = string.Empty;
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }

    public static QualityPoint Create(
        Guid id,
        string tenantId,
        string name,
        string materialId,
        string operationType,
        QualityCheckType checkType,
        string instructions,
        bool isMandatory)
    {
        var qp = new QualityPoint();
        qp.ApplyChange(new QualityPointCreatedEvent(
            id,
            tenantId,
            name,
            materialId,
            operationType,
            checkType,
            instructions,
            isMandatory,
            DateTime.UtcNow));
        return qp;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case QualityPointCreatedEvent e:
                Id = e.AggregateId;
                Name = e.Name;
                MaterialId = e.MaterialId;
                OperationType = e.OperationType;
                CheckType = e.CheckType;
                Instructions = e.Instructions;
                IsMandatory = e.IsMandatory;
                IsActive = true;
                break;
        }
    }
}

public enum QualityCheckType
{
    PassFail = 1,
    Measure = 2,
    Visual = 3
}

public record QualityPointCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string Name,
    string MaterialId,
    string OperationType,
    QualityCheckType CheckType,
    string Instructions,
    bool IsMandatory,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
