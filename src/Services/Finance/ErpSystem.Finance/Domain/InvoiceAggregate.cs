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
    public decimal Amount => this.Quantity * this.UnitPrice;
    public decimal TaxAmount => this.Amount * this.TaxRate;
    public decimal TotalAmount => this.Amount + this.TaxAmount;
}

// --- Events ---
public record InvoiceCreatedEvent(Guid InvoiceId, string InvoiceNumber, InvoiceType Type, string PartyId, string PartyName, DateTime InvoiceDate, DateTime DueDate, string Currency, Guid? StatementId) : IDomainEvent
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
    public decimal OutstandingAmount => this.TotalAmount - this.PaidAmount;

    public Guid? StatementId { get; private set; } // Link to Reconciliation Statement

    private readonly List<InvoiceLine> _lines = [];
    public IReadOnlyCollection<InvoiceLine> Lines => this._lines.AsReadOnly();

    public static Invoice Create(Guid id, string number, InvoiceType type, string partyId, string partyName, DateTime invoiceDate, DateTime dueDate, string currency, Guid? statementId = null)
    {
        Invoice invoice = new();
        invoice.ApplyChange(new InvoiceCreatedEvent(id, number, type, partyId, partyName, invoiceDate, dueDate, currency, statementId));
        return invoice;
    }

    public void UpdateLines(List<InvoiceLine> lines)
    {
        if (this.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be modified.");

        this.ApplyChange(new InvoiceLinesUpdatedEvent(this.Id, lines));
    }

    public void Issue()
    {
        if (this.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Invoice is already issued or cancelled.");
        if (this.TotalAmount <= 0)
            // ... (rest of method)
            throw new InvalidOperationException("Invoice total must be greater than zero.");

        this.ApplyChange(new InvoiceIssuedEvent(this.Id));
    }

    // ... (rest of class)

    public void RecordPayment(Guid paymentId, decimal amount, DateTime paymentDate, PaymentMethod method, string? referenceNo)
    {
        if (this.Status != InvoiceStatus.Issued && this.Status != InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Cannot record payment on non-issued invoice.");

        if (this.PaidAmount + amount > this.TotalAmount)
            throw new InvalidOperationException("Payment amount exceeds outstanding amount.");

        this.ApplyChange(new PaymentRecordedEvent(this.Id, paymentId, amount, paymentDate, method, referenceNo));
    }

    public void WriteOff(string reason)
    {
        if (this.Status == InvoiceStatus.FullyPaid || this.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot write off paid or cancelled invoice.");

        this.ApplyChange(new InvoiceStatusChangedEvent(this.Id, InvoiceStatus.WrittenOff, reason));
    }

    public void Cancel()
    {
        if (this.Status == InvoiceStatus.FullyPaid || this.Status == InvoiceStatus.WrittenOff)
            throw new InvalidOperationException("Cannot cancel paid or written-off invoice.");

        this.ApplyChange(new InvoiceStatusChangedEvent(this.Id, InvoiceStatus.Cancelled, null));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case InvoiceCreatedEvent e:
                this.Id = e.InvoiceId;
                this.InvoiceNumber = e.InvoiceNumber;
                this.Type = e.Type;
                this.PartyId = e.PartyId;
                this.PartyName = e.PartyName;
                this.InvoiceDate = e.InvoiceDate;
                this.DueDate = e.DueDate;
                this.Currency = e.Currency;
                this.StatementId = e.StatementId;
                this.Status = InvoiceStatus.Draft;
                break;
            case InvoiceLinesUpdatedEvent e:
                this._lines.Clear();
                this._lines.AddRange(e.Lines);
                this.TotalAmount = this._lines.Sum(l => l.TotalAmount);
                break;
            case InvoiceIssuedEvent:
                this.Status = InvoiceStatus.Issued;
                break;
            case PaymentRecordedEvent e:
                this.PaidAmount += e.Amount;
                this.Status = Math.Abs(this.TotalAmount - this.PaidAmount) < 0.001m ? InvoiceStatus.FullyPaid : InvoiceStatus.PartiallyPaid;

                break;
            case InvoiceStatusChangedEvent e:
                this.Status = e.NewStatus;
                break;
        }
    }
}

