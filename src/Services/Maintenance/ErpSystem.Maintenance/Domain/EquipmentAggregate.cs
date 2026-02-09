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
        Equipment equipment = new Equipment();
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
        if (this.Status == EquipmentStatus.Down) return;
        this.ApplyChange(new EquipmentStatusChangedEvent(this.Id, EquipmentStatus.Down, DateTime.UtcNow));
    }

    public void MarkAsOperational()
    {
        if (this.Status == EquipmentStatus.Operational) return;
        this.ApplyChange(new EquipmentStatusChangedEvent(this.Id, EquipmentStatus.Operational, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case EquipmentCreatedEvent e:
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.Name = e.Name;
                this.EquipmentCode = e.EquipmentCode;
                this.WorkCenterId = e.WorkCenterId;
                this.Status = EquipmentStatus.Operational;
                break;
            case EquipmentStatusChangedEvent e:
                this.Status = e.Status;
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
    public DateTime OccurredOn => this.OccurredAt;
}

public record EquipmentStatusChangedEvent(
    Guid AggregateId,
    EquipmentStatus Status,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}
