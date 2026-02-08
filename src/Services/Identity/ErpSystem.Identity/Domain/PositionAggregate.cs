using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Identity.Domain;

// Events
public record PositionCreatedEvent(Guid PositionId, string Name, string Description) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class Position : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public static Position Create(Guid id, string name, string description)
    {
        var pos = new Position();
        pos.ApplyChange(new PositionCreatedEvent(id, name, description));
        return pos;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PositionCreatedEvent e:
                Id = e.PositionId;
                Name = e.Name;
                Description = e.Description;
                break;
        }
    }
}
