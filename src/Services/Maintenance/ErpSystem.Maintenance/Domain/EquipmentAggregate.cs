using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Maintenance.Domain;

/// <summary>
/// Equipment Aggregate - Tracks machinery and assets requiring maintenance
/// </summary>
public class Equipment : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string EquipmentCode { get; private set; } = string.Empty;
    public string WorkCenterId { get; private set; } = string.Empty;
    public EquipmentStatus Status { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    public static Equipment Create(
        Guid id,
        string tenantId,
        string name,
        string equipmentCode,
        string workCenterId)
    {
        var equipment = new Equipment();
        equipment.ApplyChange(new EquipmentCreatedEvent(
            id,
            tenantId,
            name,
            equipmentCode,
            workCenterId,
            DateTime.UtcNow));
        return equipment;
    }

    public void MarkAsDown()
    {
        if (Status == EquipmentStatus.Down) return;
        ApplyChange(new EquipmentStatusChangedEvent(Id, EquipmentStatus.Down, DateTime.UtcNow));
    }

    public void MarkAsOperational()
    {
        if (Status == EquipmentStatus.Operational) return;
        ApplyChange(new EquipmentStatusChangedEvent(Id, EquipmentStatus.Operational, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case EquipmentCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                Name = e.Name;
                EquipmentCode = e.EquipmentCode;
                WorkCenterId = e.WorkCenterId;
                Status = EquipmentStatus.Operational;
                break;
            case EquipmentStatusChangedEvent e:
                Status = e.Status;
                break;
        }
    }
}

public enum EquipmentStatus
{
    Operational = 1,
    Down = 2,
    InMaintenance = 3
}

public record EquipmentCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string Name,
    string EquipmentCode,
    string WorkCenterId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record EquipmentStatusChangedEvent(
    Guid AggregateId,
    EquipmentStatus Status,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
