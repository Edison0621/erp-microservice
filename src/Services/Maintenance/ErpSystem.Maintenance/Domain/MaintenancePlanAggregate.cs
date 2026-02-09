using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Maintenance.Domain;

/// <summary>
/// Maintenance Plan - Defines recurring maintenance schedules for equipment
/// </summary>
public class MaintenancePlan : AggregateRoot<Guid>
{
    public Guid EquipmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public MaintenanceFrequency Frequency { get; private set; }
    public int IntervalValue { get; private set; }
    public DateTime? LastMaintenanceDate { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    public static MaintenancePlan Create(
        Guid id,
        string tenantId,
        Guid equipmentId,
        string name,
        MaintenanceFrequency frequency,
        int intervalValue)
    {
        MaintenancePlan plan = new MaintenancePlan();
        plan.ApplyChange(new MaintenancePlanCreatedEvent(
            id,
            tenantId,
            equipmentId,
            name,
            frequency,
            intervalValue,
            DateTime.UtcNow));
        return plan;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case MaintenancePlanCreatedEvent e:
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.EquipmentId = e.EquipmentId;
                this.Name = e.Name;
                this.Frequency = e.Frequency;
                this.IntervalValue = e.IntervalValue;
                break;
        }
    }
}

public enum MaintenanceFrequency
{
    Days = 1,
    Weeks = 2,
    Months = 3,
    UsageHours = 4
}

public record MaintenancePlanCreatedEvent(
    Guid AggregateId,
    string TenantId,
    Guid EquipmentId,
    string Name,
    MaintenanceFrequency Frequency,
    int IntervalValue,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}
