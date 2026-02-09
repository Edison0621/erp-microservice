using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

// --- Enums ---
public enum JournalEntryStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public enum JournalEntrySource
{
    Manual = 1,
    Sales = 2,
    Purchasing = 3,
    Inventory = 4,
    Production = 5,
    Payroll = 6
}

// --- Value Objects ---
public record JournalEntryLine(Guid AccountId, string AccountName, string Description, decimal Debit, decimal Credit)
{
    public decimal Amount => this.Debit - this.Credit; // Net impact
}

// --- Events ---
public record JournalEntryCreatedEvent(Guid JournalEntryId, string DocumentNumber, DateTime TransactionDate, DateTime PostingDate, string Description, JournalEntrySource Source, string? ReferenceNo) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record JournalEntryLinesUpdatedEvent(Guid JournalEntryId, List<JournalEntryLine> Lines) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record JournalEntryPostedEvent(Guid JournalEntryId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record JournalEntryVoidedEvent(Guid JournalEntryId, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate Root ---
public class JournalEntry : AggregateRoot<Guid>
{
    public string DocumentNumber { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    public DateTime PostingDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public JournalEntryStatus Status { get; private set; }
    public JournalEntrySource Source { get; private set; }
    public string? ReferenceNo { get; private set; }

    private readonly List<JournalEntryLine> _lines = [];
    public IReadOnlyCollection<JournalEntryLine> Lines => this._lines.AsReadOnly();

    public decimal TotalDebit => this._lines.Sum(l => l.Debit);
    public decimal TotalCredit => this._lines.Sum(l => l.Credit);
    public bool IsBalanced => this.TotalDebit == this.TotalCredit;

    public static JournalEntry Create(Guid id, string docNumber, DateTime transactionDate, DateTime postingDate, string description, JournalEntrySource source, string? referenceNo)
    {
        JournalEntry je = new();
        je.ApplyChange(new JournalEntryCreatedEvent(id, docNumber, transactionDate, postingDate, description, source, referenceNo));
        return je;
    }

    public void UpdateLines(List<JournalEntryLine> lines)
    {
        if (this.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Cannot update lines of a posted or voided journal entry.");

        this.ApplyChange(new JournalEntryLinesUpdatedEvent(this.Id, lines));
    }

    public void Post()
    {
        if (this.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Journal Entry is already posted or voided.");
        
        if (!this.IsBalanced)
            throw new InvalidOperationException($"Journal Entry is not balanced. Debit: {this.TotalDebit}, Credit: {this.TotalCredit}");

        if (this.TotalDebit <= 0)
            throw new InvalidOperationException("Journal Entry cannot be empty.");

        this.ApplyChange(new JournalEntryPostedEvent(this.Id));
    }

    public void Void(string reason)
    {
        if (this.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted entries can be voided.");

        this.ApplyChange(new JournalEntryVoidedEvent(this.Id, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case JournalEntryCreatedEvent e:
                this.Id = e.JournalEntryId;
                this.DocumentNumber = e.DocumentNumber;
                this.TransactionDate = e.TransactionDate;
                this.PostingDate = e.PostingDate;
                this.Description = e.Description;
                this.Source = e.Source;
                this.ReferenceNo = e.ReferenceNo;
                this.Status = JournalEntryStatus.Draft;
                break;
            case JournalEntryLinesUpdatedEvent e:
                this._lines.Clear();
                this._lines.AddRange(e.Lines);
                break;
            case JournalEntryPostedEvent:
                this.Status = JournalEntryStatus.Posted;
                break;
            case JournalEntryVoidedEvent:
                this.Status = JournalEntryStatus.Voided;
                break;
        }
    }
}
