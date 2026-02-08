using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// Events
public record WarehouseCreatedEvent(
    Guid WarehouseId, 
    string WarehouseCode, 
    string WarehouseName, 
    string Type
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class Warehouse : AggregateRoot<Guid>
{
    public string WarehouseCode { get; private set; } = string.Empty;
    public string WarehouseName { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;

    public static Warehouse Create(Guid id, string code, string name, string type)
    {
        var warehouse = new Warehouse();
        warehouse.ApplyChange(new WarehouseCreatedEvent(id, code, name, type));
        return warehouse;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WarehouseCreatedEvent e:
                Id = e.WarehouseId;
                WarehouseCode = e.WarehouseCode;
                WarehouseName = e.WarehouseName;
                Type = e.Type;
                break;
        }
    }
}
