using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Identity.Domain;

// Events
public record DepartmentCreatedEvent(Guid DepartmentId, string Name, string ParentId, int Order) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record DepartmentMovedEvent(Guid DepartmentId, string NewParentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class Department : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string ParentId { get; private set; } = string.Empty; // Guid.Empty or "ROOT" if top level
    public int Order { get; private set; }

    public static Department Create(Guid id, string name, string parentId, int order)
    {
        Department dept = new Department();
        dept.ApplyChange(new DepartmentCreatedEvent(id, name, parentId, order));
        return dept;
    }

    public void Move(string newParentId)
    {
        if (this.ParentId != newParentId)
        {
            this.ApplyChange(new DepartmentMovedEvent(this.Id, newParentId));
        }
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case DepartmentCreatedEvent e:
                this.Id = e.DepartmentId;
                this.Name = e.Name;
                this.ParentId = e.ParentId;
                this.Order = e.Order;
                break;
            case DepartmentMovedEvent e:
                this.ParentId = e.NewParentId;
                break;
        }
    }
}
