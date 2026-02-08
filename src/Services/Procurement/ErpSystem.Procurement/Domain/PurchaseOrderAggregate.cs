using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Procurement.Domain;

public enum PurchaseOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    SentToSupplier = 3,
    PartiallyReceived = 4,
    FullyReceived = 5,
    Closed = 6,
    Cancelled = 7
}

public record PurchaseOrderLine(
    string LineNumber,
    string MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal UnitPrice,
    string WarehouseId,
    DateTime RequiredDate
)
{
    public decimal TotalAmount => OrderedQuantity * UnitPrice;
}

// Events
public record PurchaseOrderCreatedEvent(
    Guid POId, 
    string PONumber, 
    string SupplierId, 
    string SupplierName, 
    DateTime OrderDate, 
    string Currency, 
    List<PurchaseOrderLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderSubmittedEvent(Guid POId) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderApprovedEvent(Guid POId, string ApprovedBy, string Comment) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderSentEvent(Guid POId, string SentBy, string Method) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record GoodsReceivedEvent(
    Guid POId, 
    Guid ReceiptId, 
    DateTime ReceiptDate, 
    string ReceivedBy, 
    List<ReceiptLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ReceiptLine(string LineNumber, decimal Quantity, string WarehouseId, string LocationId, string QualityStatus);

public record PurchaseOrderClosedEvent(Guid POId, string Reason) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderCancelledEvent(Guid POId, string Reason) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class PurchaseOrder : AggregateRoot<Guid>
{
    public string PONumber { get; private set; } = string.Empty;
    public string SupplierId { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public List<PurchaseOrderLine> Lines { get; private set; } = new();
    public decimal TotalAmount => Lines.Sum(l => l.TotalAmount);

    public static PurchaseOrder Create(
        Guid id, 
        string poNumber, 
        string supplierId, 
        string supplierName, 
        DateTime orderDate, 
        string currency, 
        List<PurchaseOrderLine> lines)
    {
        var po = new PurchaseOrder();
        po.ApplyChange(new PurchaseOrderCreatedEvent(id, poNumber, supplierId, supplierName, orderDate, currency, lines));
        return po;
    }

    public void Submit()
    {
        if (Status != PurchaseOrderStatus.Draft) throw new InvalidOperationException("Only Draft PO can be submitted");
        ApplyChange(new PurchaseOrderSubmittedEvent(Id));
    }

    public void Approve(string approvedBy, string comment)
    {
        if (Status != PurchaseOrderStatus.PendingApproval) throw new InvalidOperationException("Non-pending PO cannot be approved");
        ApplyChange(new PurchaseOrderApprovedEvent(Id, approvedBy, comment));
    }

    public void Send(string sentBy, string method)
    {
        if (Status != PurchaseOrderStatus.Approved) throw new InvalidOperationException("Only Approved PO can be sent");
        ApplyChange(new PurchaseOrderSentEvent(Id, sentBy, method));
    }

    public void RecordReceipt(Guid receiptId, DateTime receiptDate, string receivedBy, List<ReceiptLine> receiptLines)
    {
        if (Status != PurchaseOrderStatus.SentToSupplier && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException("PO status does not allow receipt");

        // Simple validation: check if line numbers exist
        foreach (var rl in receiptLines)
        {
            if (!Lines.Any(l => l.LineNumber == rl.LineNumber))
                throw new Exception($"Line Number {rl.LineNumber} not found in PO");
        }

        ApplyChange(new GoodsReceivedEvent(Id, receiptId, receiptDate, receivedBy, receiptLines));
    }

    public void Close(string reason)
    {
        ApplyChange(new PurchaseOrderClosedEvent(Id, reason));
    }

    public void Cancel(string reason)
    {
        if (Status != PurchaseOrderStatus.Draft && Status != PurchaseOrderStatus.PendingApproval && Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException("PO cannot be cancelled at this stage");
        
        if (Lines.Any(l => l.ReceivedQuantity > 0))
            throw new InvalidOperationException("Cannot cancel PO with received goods");

        ApplyChange(new PurchaseOrderCancelledEvent(Id, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PurchaseOrderCreatedEvent e:
                Id = e.POId;
                PONumber = e.PONumber;
                SupplierId = e.SupplierId;
                SupplierName = e.SupplierName;
                Currency = e.Currency;
                Lines = e.Lines;
                Status = PurchaseOrderStatus.Draft;
                break;
            case PurchaseOrderSubmittedEvent:
                Status = PurchaseOrderStatus.PendingApproval;
                break;
            case PurchaseOrderApprovedEvent:
                Status = PurchaseOrderStatus.Approved;
                break;
            case PurchaseOrderSentEvent:
                Status = PurchaseOrderStatus.SentToSupplier;
                break;
            case GoodsReceivedEvent e:
                foreach (var rl in e.Lines)
                {
                    var line = Lines.First(l => l.LineNumber == rl.LineNumber);
                    var updatedLine = line with { ReceivedQuantity = line.ReceivedQuantity + rl.Quantity };
                    Lines[Lines.FindIndex(l => l.LineNumber == rl.LineNumber)] = updatedLine;
                }
                
                if (Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity))
                    Status = PurchaseOrderStatus.FullyReceived;
                else
                    Status = PurchaseOrderStatus.PartiallyReceived;
                break;
            case PurchaseOrderClosedEvent:
                Status = PurchaseOrderStatus.Closed;
                break;
            case PurchaseOrderCancelledEvent:
                Status = PurchaseOrderStatus.Cancelled;
                break;
        }
    }
}
