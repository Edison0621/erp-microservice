using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

public enum PaymentDirection { Incoming = 1, Outgoing = 2 } // Incoming = AR, Outgoing = AP

public enum PaymentStatus { Draft, Completed, Failed, Voided }

public record PaymentCreatedEvent(
    Guid PaymentId, 
    string PaymentNumber, 
    PaymentDirection Direction, 
    string PartyId, 
    string PartyName, 
    decimal Amount, 
    string Currency, 
    DateTime PaymentDate, 
    PaymentMethod Method, 
    string? ReferenceNo
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PaymentAllocatedEvent(Guid PaymentId, Guid InvoiceId, decimal AllocationAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PaymentCompletedEvent(Guid PaymentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class Payment : AggregateRoot<Guid>
{
    public string PaymentNumber { get; private set; } = string.Empty;
    public PaymentDirection Direction { get; private set; }
    public string PartyId { get; private set; } = string.Empty;
    public string PartyName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal UnallocatedAmount { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public DateTime PaymentDate { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? ReferenceNo { get; private set; }
    public PaymentStatus Status { get; private set; }

    private readonly Dictionary<Guid, decimal> _allocations = new();
    public IReadOnlyDictionary<Guid, decimal> Allocations => this._allocations;

    public static Payment Create(
        Guid id, 
        string number, 
        PaymentDirection direction, 
        string partyId, 
        string partyName, 
        decimal amount, 
        string currency, 
        DateTime date, 
        PaymentMethod method, 
        string? reference)
    {
        Payment p = new();
        p.ApplyChange(new PaymentCreatedEvent(id, number, direction, partyId, partyName, amount, currency, date, method, reference));
        return p;
    }

    public void AllocateToInvoice(Guid invoiceId, decimal amount)
    {
        if (this.Status != PaymentStatus.Draft && this.Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot allocate voided or failed payments.");
        
        if (amount > this.UnallocatedAmount)
            throw new InvalidOperationException("Allocation amount exceeds unallocated balance.");

        this.ApplyChange(new PaymentAllocatedEvent(this.Id, invoiceId, amount));
    }

    public void Complete()
    {
        if (this.Status == PaymentStatus.Completed) return;
        this.ApplyChange(new PaymentCompletedEvent(this.Id));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaymentCreatedEvent e:
                this.Id = e.PaymentId;
                this.PaymentNumber = e.PaymentNumber;
                this.Direction = e.Direction;
                this.PartyId = e.PartyId;
                this.PartyName = e.PartyName;
                this.Amount = e.Amount;
                this.UnallocatedAmount = e.Amount;
                this.Currency = e.Currency;
                this.PaymentDate = e.PaymentDate;
                this.Method = e.Method;
                this.ReferenceNo = e.ReferenceNo;
                this.Status = PaymentStatus.Draft;
                break;
            case PaymentAllocatedEvent e:
                this.UnallocatedAmount -= e.AllocationAmount;
                if (this._allocations.ContainsKey(e.InvoiceId))
                    this._allocations[e.InvoiceId] += e.AllocationAmount;
                else
                    this._allocations[e.InvoiceId] = e.AllocationAmount;
                
                // Auto-complete if fully allocated? Maybe not, keep explicit.
                break;
            case PaymentCompletedEvent:
                this.Status = PaymentStatus.Completed;
                break;
        }
    }
}
