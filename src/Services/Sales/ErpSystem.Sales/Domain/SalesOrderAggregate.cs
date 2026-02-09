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
    public decimal LineAmount => this.OrderedQuantity * this.UnitPrice * (1 - this.DiscountRate);
}

// Events
public record SalesOrderCreatedEvent(
    Guid OrderId,
    string SoNumber,
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
    public string SoNumber { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public SalesOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public List<SalesOrderLine> Lines { get; private set; } = [];
    public decimal TotalAmount => this.Lines.Sum(l => l.LineAmount);

    public static SalesOrder Create(
        Guid id, 
        string soNumber, 
        string customerId, 
        string customerName, 
        DateTime orderDate, 
        string currency, 
        List<SalesOrderLine> lines)
    {
        SalesOrder so = new SalesOrder();
        so.ApplyChange(new SalesOrderCreatedEvent(id, soNumber, customerId, customerName, orderDate, currency, lines));
        return so;
    }

    public void Confirm()
    {
        if (this.Status != SalesOrderStatus.Draft && this.Status != SalesOrderStatus.PendingApproval)
            throw new InvalidOperationException("Only Draft or Pending orders can be confirmed");
        this.ApplyChange(new SalesOrderConfirmedEvent(this.Id));
    }

    public void Cancel(string reason)
    {
        if (this.Status == SalesOrderStatus.FullyShipped || this.Status == SalesOrderStatus.Closed)
            throw new InvalidOperationException("Cannot cancel a shipped or closed order");
        this.ApplyChange(new SalesOrderCancelledEvent(this.Id, reason));
    }

    public void ProcessShipment(Guid shipmentId, List<ShipmentProcessedLine> shipmentLines)
    {
        if (this.Status != SalesOrderStatus.Confirmed && this.Status != SalesOrderStatus.PartiallyShipped)
            throw new InvalidOperationException("Order must be confirmed or partially shipped to process shipment");

        this.ApplyChange(new SalesOrderShipmentProcessedEvent(this.Id, shipmentId, shipmentLines));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SalesOrderCreatedEvent e:
                this.Id = e.OrderId;
                this.SoNumber = e.SoNumber;
                this.CustomerId = e.CustomerId;
                this.CustomerName = e.CustomerName;
                this.Currency = e.Currency;
                this.Lines = e.Lines;
                this.Status = SalesOrderStatus.Draft;
                break;
            case SalesOrderConfirmedEvent:
                this.Status = SalesOrderStatus.Confirmed;
                break;
            case SalesOrderCancelledEvent:
                this.Status = SalesOrderStatus.Cancelled;
                break;
            case SalesOrderShipmentProcessedEvent e:
                foreach (ShipmentProcessedLine sl in e.Lines)
                {
                    int lineIdx = this.Lines.FindIndex(l => l.LineNumber == sl.LineNumber);
                    if (lineIdx >= 0)
                    {
                        SalesOrderLine line = this.Lines[lineIdx];
                        this.Lines[lineIdx] = line with { ShippedQuantity = line.ShippedQuantity + sl.ShippedQuantity };
                    }
                }
                
                this.Status = this.Lines.All(l => l.ShippedQuantity >= l.OrderedQuantity) ? SalesOrderStatus.FullyShipped : SalesOrderStatus.PartiallyShipped;
                break;
        }
    }
}
