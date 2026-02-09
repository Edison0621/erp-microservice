using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Sales.Domain;

public record ShipmentLine(string LineNumber, string MaterialId, decimal ShippedQuantity);

public record ShipmentCreatedEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid SalesOrderId,
    string SoNumber,
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
    public string SoNumber { get; private set; } = string.Empty;
    public DateTime ShippedDate { get; private set; }
    public string ShippedBy { get; private set; } = string.Empty;
    public string WarehouseId { get; private set; } = string.Empty;
    public List<ShipmentLine> Lines { get; private set; } = [];

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
        Shipment shipment = new();
        shipment.ApplyChange(new ShipmentCreatedEvent(id, shipmentNumber, salesOrderId, soNumber, shippedDate, shippedBy, warehouseId, lines));
        return shipment;
    }

    protected override void Apply(IDomainEvent @event)
    {
        if (@event is ShipmentCreatedEvent e)
        {
            this.Id = e.ShipmentId;
            this.ShipmentNumber = e.ShipmentNumber;
            this.SalesOrderId = e.SalesOrderId;
            this.SoNumber = e.SoNumber;
            this.ShippedDate = e.ShippedDate;
            this.ShippedBy = e.ShippedBy;
            this.WarehouseId = e.WarehouseId;
            this.Lines = e.Lines;
        }
    }
}
