using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Sales.Domain;

public record ShipmentLine(string LineNumber, string MaterialId, decimal ShippedQuantity);

public record ShipmentCreatedEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid SalesOrderId,
    string SONumber,
    DateTime ShippedDate,
    string ShippedBy,
    string WarehouseId,
    List<ShipmentLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class Shipment : AggregateRoot<Guid>
{
    public string ShipmentNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public string SONumber { get; private set; } = string.Empty;
    public DateTime ShippedDate { get; private set; }
    public string ShippedBy { get; private set; } = string.Empty;
    public string WarehouseId { get; private set; } = string.Empty;
    public List<ShipmentLine> Lines { get; private set; } = new();

    public static Shipment Create(
        Guid id, 
        string shipmentNumber, 
        Guid salesOrderId, 
        string soNumber, 
        DateTime shippedDate, 
        string shippedBy, 
        string warehouseId, 
        List<ShipmentLine> lines)
    {
        var shipment = new Shipment();
        shipment.ApplyChange(new ShipmentCreatedEvent(id, shipmentNumber, salesOrderId, soNumber, shippedDate, shippedBy, warehouseId, lines));
        return shipment;
    }

    protected override void Apply(IDomainEvent @event)
    {
        if (@event is ShipmentCreatedEvent e)
        {
            Id = e.ShipmentId;
            ShipmentNumber = e.ShipmentNumber;
            SalesOrderId = e.SalesOrderId;
            SONumber = e.SONumber;
            ShippedDate = e.ShippedDate;
            ShippedBy = e.ShippedBy;
            WarehouseId = e.WarehouseId;
            Lines = e.Lines;
        }
    }
}
