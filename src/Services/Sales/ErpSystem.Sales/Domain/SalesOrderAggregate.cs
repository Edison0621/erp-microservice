using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Sales.Domain;

public enum SalesOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Confirmed = 2,
    PartiallyShipped = 3,
    FullyShipped = 4,
    Closed = 5,
    Cancelled = 6
}

public record SalesOrderLine(
    string LineNumber,
    string MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal OrderedQuantity,
    decimal ShippedQuantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal DiscountRate
)
{
    public decimal LineAmount => OrderedQuantity * UnitPrice * (1 - DiscountRate);
}

// Events
public record SalesOrderCreatedEvent(
    Guid OrderId,
    string SONumber,
    string CustomerId,
    string CustomerName,
    DateTime OrderDate,
    string Currency,
    List<SalesOrderLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SalesOrderConfirmedEvent(Guid OrderId) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SalesOrderCancelledEvent(Guid OrderId, string Reason) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SalesOrderShipmentProcessedEvent(
    Guid OrderId,
    Guid ShipmentId,
    List<ShipmentProcessedLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ShipmentProcessedLine(string LineNumber, decimal ShippedQuantity);

// Aggregate
public class SalesOrder : AggregateRoot<Guid>
{
    public string SONumber { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public SalesOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public List<SalesOrderLine> Lines { get; private set; } = new();
    public decimal TotalAmount => Lines.Sum(l => l.LineAmount);

    public static SalesOrder Create(
        Guid id, 
        string soNumber, 
        string customerId, 
        string customerName, 
        DateTime orderDate, 
        string currency, 
        List<SalesOrderLine> lines)
    {
        var so = new SalesOrder();
        so.ApplyChange(new SalesOrderCreatedEvent(id, soNumber, customerId, customerName, orderDate, currency, lines));
        return so;
    }

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.PendingApproval)
            throw new InvalidOperationException("Only Draft or Pending orders can be confirmed");
        ApplyChange(new SalesOrderConfirmedEvent(Id));
    }

    public void Cancel(string reason)
    {
        if (Status == SalesOrderStatus.FullyShipped || Status == SalesOrderStatus.Closed)
            throw new InvalidOperationException("Cannot cancel a shipped or closed order");
        ApplyChange(new SalesOrderCancelledEvent(Id, reason));
    }

    public void ProcessShipment(Guid shipmentId, List<ShipmentProcessedLine> shipmentLines)
    {
        if (Status != SalesOrderStatus.Confirmed && Status != SalesOrderStatus.PartiallyShipped)
            throw new InvalidOperationException("Order must be confirmed or partially shipped to process shipment");
            
        ApplyChange(new SalesOrderShipmentProcessedEvent(Id, shipmentId, shipmentLines));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SalesOrderCreatedEvent e:
                Id = e.OrderId;
                SONumber = e.SONumber;
                CustomerId = e.CustomerId;
                CustomerName = e.CustomerName;
                Currency = e.Currency;
                Lines = e.Lines;
                Status = SalesOrderStatus.Draft;
                break;
            case SalesOrderConfirmedEvent:
                Status = SalesOrderStatus.Confirmed;
                break;
            case SalesOrderCancelledEvent:
                Status = SalesOrderStatus.Cancelled;
                break;
            case SalesOrderShipmentProcessedEvent e:
                foreach (var sl in e.Lines)
                {
                    var lineIdx = Lines.FindIndex(l => l.LineNumber == sl.LineNumber);
                    if (lineIdx >= 0)
                    {
                        var line = Lines[lineIdx];
                        Lines[lineIdx] = line with { ShippedQuantity = line.ShippedQuantity + sl.ShippedQuantity };
                    }
                }
                
                if (Lines.All(l => l.ShippedQuantity >= l.OrderedQuantity))
                    Status = SalesOrderStatus.FullyShipped;
                else
                    Status = SalesOrderStatus.PartiallyShipped;
                break;
        }
    }
}
