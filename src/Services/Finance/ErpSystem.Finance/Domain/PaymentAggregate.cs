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
    public IReadOnlyDictionary<Guid, decimal> Allocations => _allocations;

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
        var p = new Payment();
        p.ApplyChange(new PaymentCreatedEvent(id, number, direction, partyId, partyName, amount, currency, date, method, reference));
        return p;
    }

    public void AllocateToInvoice(Guid invoiceId, decimal amount)
    {
        if (Status != PaymentStatus.Draft && Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot allocate voided or failed payments.");
        
        if (amount > UnallocatedAmount)
            throw new InvalidOperationException("Allocation amount exceeds unallocated balance.");

        ApplyChange(new PaymentAllocatedEvent(Id, invoiceId, amount));
    }

    public void Complete()
    {
        if (Status == PaymentStatus.Completed) return;
        ApplyChange(new PaymentCompletedEvent(Id));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaymentCreatedEvent e:
                Id = e.PaymentId;
                PaymentNumber = e.PaymentNumber;
                Direction = e.Direction;
                PartyId = e.PartyId;
                PartyName = e.PartyName;
                Amount = e.Amount;
                UnallocatedAmount = e.Amount;
                Currency = e.Currency;
                PaymentDate = e.PaymentDate;
                Method = e.Method;
                ReferenceNo = e.ReferenceNo;
                Status = PaymentStatus.Draft;
                break;
            case PaymentAllocatedEvent e:
                UnallocatedAmount -= e.AllocationAmount;
                if (_allocations.ContainsKey(e.InvoiceId))
                    _allocations[e.InvoiceId] += e.AllocationAmount;
                else
                    _allocations[e.InvoiceId] = e.AllocationAmount;
                
                // Auto-complete if fully allocated? Maybe not, keep explicit.
                break;
            case PaymentCompletedEvent:
                Status = PaymentStatus.Completed;
                break;
        }
    }
}
