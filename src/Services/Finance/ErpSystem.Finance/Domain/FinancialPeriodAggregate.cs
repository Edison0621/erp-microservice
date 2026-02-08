using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

// --- Events ---
public record FinancialPeriodDefinedEvent(Guid PeriodId, int Year, int PeriodNumber, DateTime StartDate, DateTime EndDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FinancialPeriodClosedEvent(Guid PeriodId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FinancialPeriodReopenedEvent(Guid PeriodId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate Root ---
public class FinancialPeriod : AggregateRoot<Guid>
{
    public int Year { get; private set; }
    public int PeriodNumber { get; private set; } // 1-12
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsClosed { get; private set; }

    public static FinancialPeriod Define(Guid id, int year, int number, DateTime start, DateTime end)
    {
        var period = new FinancialPeriod();
        period.ApplyChange(new FinancialPeriodDefinedEvent(id, year, number, start, end));
        return period;
    }

    public void Close()
    {
        if (IsClosed) return;
        ApplyChange(new FinancialPeriodClosedEvent(Id));
    }

    public void Reopen()
    {
        if (!IsClosed) return;
        ApplyChange(new FinancialPeriodReopenedEvent(Id));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case FinancialPeriodDefinedEvent e:
                Id = e.PeriodId;
                Year = e.Year;
                PeriodNumber = e.PeriodNumber;
                StartDate = e.StartDate;
                EndDate = e.EndDate;
                IsClosed = false;
                break;
            case FinancialPeriodClosedEvent:
                IsClosed = true;
                break;
            case FinancialPeriodReopenedEvent:
                IsClosed = false;
                break;
        }
    }
}
