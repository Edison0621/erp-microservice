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
    public decimal TotalAmount => this.OrderedQuantity * this.UnitPrice;
}

// Events
public record PurchaseOrderCreatedEvent(
    Guid PoId, 
    string PoNumber, 
    string SupplierId, 
    string SupplierName, 
    DateTime OrderDate, 
    string Currency, 
    List<PurchaseOrderLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderSubmittedEvent(Guid PoId) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderApprovedEvent(Guid PoId, string ApprovedBy, string Comment) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderSentEvent(Guid PoId, string SentBy, string Method) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record GoodsReceivedEvent(
    Guid PoId, 
    Guid ReceiptId, 
    DateTime ReceiptDate, 
    string ReceivedBy, 
    List<ReceiptLine> Lines
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ReceiptLine(string LineNumber, decimal Quantity, string WarehouseId, string LocationId, string QualityStatus);

public record PurchaseOrderClosedEvent(Guid PoId, string Reason) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PurchaseOrderCancelledEvent(Guid PoId, string Reason) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class PurchaseOrder : AggregateRoot<Guid>
{
    public string PoNumber { get; private set; } = string.Empty;
    public string SupplierId { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public List<PurchaseOrderLine> Lines { get; private set; } = [];
    public decimal TotalAmount => this.Lines.Sum(l => l.TotalAmount);

    public static PurchaseOrder Create(
        Guid id, 
        string poNumber, 
        string supplierId, 
        string supplierName, 
        DateTime orderDate, 
        string currency, 
        List<PurchaseOrderLine> lines)
    {
        PurchaseOrder po = new PurchaseOrder();
        po.ApplyChange(new PurchaseOrderCreatedEvent(id, poNumber, supplierId, supplierName, orderDate, currency, lines));
        return po;
    }

    public void Submit()
    {
        if (this.Status != PurchaseOrderStatus.Draft) throw new InvalidOperationException("Only Draft PO can be submitted");
        this.ApplyChange(new PurchaseOrderSubmittedEvent(this.Id));
    }

    public void Approve(string approvedBy, string comment)
    {
        if (this.Status != PurchaseOrderStatus.PendingApproval) throw new InvalidOperationException("Non-pending PO cannot be approved");
        this.ApplyChange(new PurchaseOrderApprovedEvent(this.Id, approvedBy, comment));
    }

    public void Send(string sentBy, string method)
    {
        if (this.Status != PurchaseOrderStatus.Approved) throw new InvalidOperationException("Only Approved PO can be sent");
        this.ApplyChange(new PurchaseOrderSentEvent(this.Id, sentBy, method));
    }

    public void RecordReceipt(Guid receiptId, DateTime receiptDate, string receivedBy, List<ReceiptLine> receiptLines)
    {
        if (this.Status != PurchaseOrderStatus.SentToSupplier && this.Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException("PO status does not allow receipt");

        // Simple validation: check if line numbers exist
        foreach (ReceiptLine rl in receiptLines)
        {
            if (this.Lines.All(l => l.LineNumber != rl.LineNumber))
                throw new Exception($"Line Number {rl.LineNumber} not found in PO");
        }

        this.ApplyChange(new GoodsReceivedEvent(this.Id, receiptId, receiptDate, receivedBy, receiptLines));
    }

    public void Close(string reason)
    {
        this.ApplyChange(new PurchaseOrderClosedEvent(this.Id, reason));
    }

    public void Cancel(string reason)
    {
        if (this.Status != PurchaseOrderStatus.Draft && this.Status != PurchaseOrderStatus.PendingApproval && this.Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException("PO cannot be cancelled at this stage");
        
        if (this.Lines.Any(l => l.ReceivedQuantity > 0))
            throw new InvalidOperationException("Cannot cancel PO with received goods");

        this.ApplyChange(new PurchaseOrderCancelledEvent(this.Id, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PurchaseOrderCreatedEvent e:
                this.Id = e.PoId;
                this.PoNumber = e.PoNumber;
                this.SupplierId = e.SupplierId;
                this.SupplierName = e.SupplierName;
                this.Currency = e.Currency;
                this.Lines = e.Lines;
                this.Status = PurchaseOrderStatus.Draft;
                break;
            case PurchaseOrderSubmittedEvent:
                this.Status = PurchaseOrderStatus.PendingApproval;
                break;
            case PurchaseOrderApprovedEvent:
                this.Status = PurchaseOrderStatus.Approved;
                break;
            case PurchaseOrderSentEvent:
                this.Status = PurchaseOrderStatus.SentToSupplier;
                break;
            case GoodsReceivedEvent e:
                foreach (ReceiptLine rl in e.Lines)
                {
                    PurchaseOrderLine line = this.Lines.First(l => l.LineNumber == rl.LineNumber);
                    PurchaseOrderLine updatedLine = line with { ReceivedQuantity = line.ReceivedQuantity + rl.Quantity };
                    this.Lines[this.Lines.FindIndex(l => l.LineNumber == rl.LineNumber)] = updatedLine;
                }
                
                this.Status = this.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity) ? PurchaseOrderStatus.FullyReceived : PurchaseOrderStatus.PartiallyReceived;
                break;
            case PurchaseOrderClosedEvent:
                this.Status = PurchaseOrderStatus.Closed;
                break;
            case PurchaseOrderCancelledEvent:
                this.Status = PurchaseOrderStatus.Cancelled;
                break;
        }
    }
}
