using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Events ---

public record LocationCreatedEvent(
    Guid LocationId, 
    Guid WarehouseId, 
    string Code, 
    string Name, 
    string Type
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate ---

public class WarehouseLocation : AggregateRoot<Guid>
{
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // Area, Shelf, Bin...

    public static WarehouseLocation Create(Guid id, Guid warehouseId, string code, string name, string type)
    {
        WarehouseLocation location = new WarehouseLocation();
        location.ApplyChange(new LocationCreatedEvent(id, warehouseId, code, name, type));
        return location;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case LocationCreatedEvent e:
                this.Id = e.LocationId;
                this.WarehouseId = e.WarehouseId;
                this.Code = e.Code;
                this.Name = e.Name;
                this.Type = e.Type;
                break;
        }
    }
}
