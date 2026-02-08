using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

// --- Enums ---
public enum InvoiceType { AccountsReceivable = 1, AccountsPayable = 2 }

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    FullyPaid = 3,
    WrittenOff = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer,
    Cheque,
    ElectronicPayment,
    Other
}

// --- Value Objects ---
public record InvoiceLine(string LineNumber, string? MaterialId, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate)
{
    public decimal Amount => Quantity * UnitPrice;
    public decimal TaxAmount => Amount * TaxRate;
    public decimal TotalAmount => Amount + TaxAmount;
}

// --- Events ---
public record InvoiceCreatedEvent(Guid InvoiceId, string InvoiceNumber, InvoiceType Type, string PartyId, string PartyName, DateTime InvoiceDate, DateTime DueDate, string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record InvoiceLinesUpdatedEvent(Guid InvoiceId, List<InvoiceLine> Lines) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record InvoiceIssuedEvent(Guid InvoiceId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PaymentRecordedEvent(Guid InvoiceId, Guid PaymentId, decimal Amount, DateTime PaymentDate, PaymentMethod Method, string? ReferenceNo) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record InvoiceStatusChangedEvent(Guid InvoiceId, InvoiceStatus NewStatus, string? Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate Root ---
public class Invoice : AggregateRoot<Guid>
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceType Type { get; private set; }
    public string PartyId { get; private set; } = string.Empty;
    public string PartyName { get; private set; } = string.Empty;
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public InvoiceStatus Status { get; private set; }
    
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    private readonly List<InvoiceLine> _lines = new();
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public static Invoice Create(Guid id, string number, InvoiceType type, string partyId, string partyName, DateTime invoiceDate, DateTime dueDate, string currency)
    {
        var invoice = new Invoice();
        invoice.ApplyChange(new InvoiceCreatedEvent(id, number, type, partyId, partyName, invoiceDate, dueDate, currency));
        return invoice;
    }

    public void UpdateLines(List<InvoiceLine> lines)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be modified.");
        
        ApplyChange(new InvoiceLinesUpdatedEvent(Id, lines));
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Invoice is already issued or cancelled.");
        if (TotalAmount <= 0)
            throw new InvalidOperationException("Invoice total must be greater than zero.");
        
        ApplyChange(new InvoiceIssuedEvent(Id));
    }

    public void RecordPayment(Guid paymentId, decimal amount, DateTime date, PaymentMethod method, string? reference)
    {
        if (Status == InvoiceStatus.Cancelled || Status == InvoiceStatus.WrittenOff || Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot record payment for this invoice status.");
        
        if (amount <= 0) throw new ArgumentException("Payment amount must be positive.");
        
        // Allow overpayment? For now, no.
        if (PaidAmount + amount > TotalAmount) 
            throw new InvalidOperationException($"Payment amount {amount} exceeds outstanding amount {OutstandingAmount}.");

        ApplyChange(new PaymentRecordedEvent(Id, paymentId, amount, date, method, reference));
    }

    public void WriteOff(string reason)
    {
        if (Status != InvoiceStatus.Issued && Status != InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Only issued or partially paid invoices can be written off.");
        
        ApplyChange(new InvoiceStatusChangedEvent(Id, InvoiceStatus.WrittenOff, reason));
    }

    public void Cancel()
    {
        if (Status != InvoiceStatus.Draft && Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Cannot cancel an invoice that has payments or is already closed.");
        if (PaidAmount > 0)
            throw new InvalidOperationException("Cannot cancel an invoice with payments.");

        ApplyChange(new InvoiceStatusChangedEvent(Id, InvoiceStatus.Cancelled, "Manual Cancel"));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case InvoiceCreatedEvent e:
                Id = e.InvoiceId;
                InvoiceNumber = e.InvoiceNumber;
                Type = e.Type;
                PartyId = e.PartyId;
                PartyName = e.PartyName;
                InvoiceDate = e.InvoiceDate;
                DueDate = e.DueDate;
                Currency = e.Currency;
                Status = InvoiceStatus.Draft;
                break;
            case InvoiceLinesUpdatedEvent e:
                _lines.Clear();
                _lines.AddRange(e.Lines);
                TotalAmount = _lines.Sum(l => l.TotalAmount);
                break;
            case InvoiceIssuedEvent:
                Status = InvoiceStatus.Issued;
                break;
            case PaymentRecordedEvent e:
                PaidAmount += e.Amount;
                if (Math.Abs(TotalAmount - PaidAmount) < 0.001m) // Tolerance for decimal
                {
                     Status = InvoiceStatus.FullyPaid;
                }
                else
                {
                     Status = InvoiceStatus.PartiallyPaid;
                }
                break;
            case InvoiceStatusChangedEvent e:
                Status = e.NewStatus;
                break;
        }
    }
}
