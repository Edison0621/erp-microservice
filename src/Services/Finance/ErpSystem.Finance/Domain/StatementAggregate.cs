using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

public enum StatementStatus
{
    Open = 0,
    Reconciled = 1, // Ready for Invoicing
    Invoiced = 2,
    Closed = 3
}

public enum StatementLineType
{
    GoodsReceived = 1,
    GoodsReturned = 2
}

public record StatementLine(
    Guid SourceId, // ReceiptId or ReturnId
    string SourceNumber,
    DateTime Date,
    StatementLineType Type,
    string MaterialId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount // Positive for Received, Negative for Returned
);

public record StatementCreatedEvent(Guid StatementId, string SupplierId, string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StatementLineAddedEvent(Guid StatementId, StatementLine Line) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StatementReconciledEvent(Guid StatementId, decimal TotalAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class Statement : AggregateRoot<Guid>
{
    public string SupplierId { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "CNY";
    public StatementStatus Status { get; private set; }

    private readonly List<StatementLine> _lines = [];
    public IReadOnlyCollection<StatementLine> Lines => this._lines.AsReadOnly();

    public decimal TotalAmount => this._lines.Sum(l => l.Amount);

    public static Statement Create(Guid id, string supplierId, string currency)
    {
        Statement stmt = new();
        stmt.ApplyChange(new StatementCreatedEvent(id, supplierId, currency));
        return stmt;
    }

    public void AddLine(StatementLine line)
    {
        if (this.Status != StatementStatus.Open)
            throw new InvalidOperationException("Cannot add lines to a non-open statement");

        // Simple duplicate check
        if (this._lines.Any(l => l.SourceId == line.SourceId && l.MaterialId == line.MaterialId && l.Type == line.Type))
            return; // Already processed? or throw? For idempotency, maybe just return.

        this.ApplyChange(new StatementLineAddedEvent(this.Id, line));
    }

    public void Reconcile()
    {
        if (this.Status != StatementStatus.Open)
            throw new InvalidOperationException("Statement is not open");

        if (!this._lines.Any())
            throw new InvalidOperationException("Cannot reconcile empty statement");

        this.ApplyChange(new StatementReconciledEvent(this.Id, this.TotalAmount));
    }

    public void MarkInvoiced()
    {
        if (this.Status != StatementStatus.Reconciled)
            throw new InvalidOperationException("Statement must be reconciled before invoicing");

        // Transition to Invoiced? Or keep Reconciled? 
        // Let's assume Invoiced means an Invoice has been generated from it.
        // We might need an event for this, but for now just validation method or implicit property setter?
        // Let's add explicit method if we want to lock it further.
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case StatementCreatedEvent e:
                this.Id = e.StatementId;
                this.SupplierId = e.SupplierId;
                this.Currency = e.Currency;
                this.Status = StatementStatus.Open;
                break;
            case StatementLineAddedEvent e:
                this._lines.Add(e.Line);
                break;
            case StatementReconciledEvent:
                this.Status = StatementStatus.Reconciled;
                break;
        }
    }
}
