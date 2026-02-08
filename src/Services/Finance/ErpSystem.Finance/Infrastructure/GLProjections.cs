using MediatR;
using ErpSystem.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Infrastructure;

public class GLProjections : 
    INotificationHandler<AccountCreatedEvent>,
    INotificationHandler<AccountDetailsUpdatedEvent>,
    INotificationHandler<JournalEntryCreatedEvent>,
    INotificationHandler<JournalEntryLinesUpdatedEvent>,
    INotificationHandler<JournalEntryPostedEvent>,
    INotificationHandler<JournalEntryVoidedEvent>,
    INotificationHandler<FinancialPeriodDefinedEvent>,
    INotificationHandler<FinancialPeriodClosedEvent>,
    INotificationHandler<FinancialPeriodReopenedEvent>
{
    private readonly FinanceReadDbContext _db;

    public GLProjections(FinanceReadDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AccountCreatedEvent e, CancellationToken ct)
    {
        var account = new AccountReadModel
        {
            AccountId = e.AccountId,
            Code = e.Code,
            Name = e.Name,
            Type = (int)e.Type,
            Class = (int)e.Class,
            ParentAccountId = e.ParentAccountId,
            BalanceType = (int)e.BalanceType,
            Currency = e.Currency,
            IsActive = true
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(AccountDetailsUpdatedEvent e, CancellationToken ct)
    {
        var account = await _db.Accounts.FindAsync(new object[] { e.AccountId }, ct);
        if (account != null)
        {
            account.Name = e.Name;
            account.IsActive = e.IsActive;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(JournalEntryCreatedEvent e, CancellationToken ct)
    {
        var je = new JournalEntryReadModel
        {
            JournalEntryId = e.JournalEntryId,
            DocumentNumber = e.DocumentNumber,
            TransactionDate = e.TransactionDate,
            PostingDate = e.PostingDate,
            Description = e.Description,
            Source = (int)e.Source,
            ReferenceNo = e.ReferenceNo,
            Status = (int)JournalEntryStatus.Draft
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(JournalEntryLinesUpdatedEvent e, CancellationToken ct)
    {
        // Remove existing lines
        var existingLines = _db.JournalEntryLines.Where(l => l.JournalEntryId == e.JournalEntryId);
        _db.JournalEntryLines.RemoveRange(existingLines);

        // Add new lines
        // We need Account Names for the Read Model. 
        // Ideally we fetch them, or we just store empty for now if performance is critical 
        // (and join later in Query).
        // Let's try to fetch Account Names for better Read Model usability.
        var accountIds = e.Lines.Select(x => x.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts.Where(a => accountIds.Contains(a.AccountId)).ToDictionaryAsync(a => a.AccountId, a => a.Name, ct);

        foreach (var line in e.Lines)
        {
            var accountName = accounts.TryGetValue(line.AccountId, out var name) ? name : "Unknown";
            _db.JournalEntryLines.Add(new JournalEntryLineReadModel
            {
                JournalEntryId = e.JournalEntryId,
                AccountId = line.AccountId,
                AccountName = accountName,
                Description = line.Description,
                Debit = line.Debit,
                Credit = line.Credit
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(JournalEntryPostedEvent e, CancellationToken ct)
    {
        var je = await _db.JournalEntries.FindAsync(new object[] { e.JournalEntryId }, ct);
        if (je != null)
        {
            je.Status = (int)JournalEntryStatus.Posted;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(JournalEntryVoidedEvent e, CancellationToken ct)
    {
        var je = await _db.JournalEntries.FindAsync(new object[] { e.JournalEntryId }, ct);
        if (je != null)
        {
            je.Status = (int)JournalEntryStatus.Voided;
            // Should update description or status to reflect reason?
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(FinancialPeriodDefinedEvent e, CancellationToken ct)
    {
        var period = new FinancialPeriodReadModel
        {
            PeriodId = e.PeriodId,
            Year = e.Year,
            PeriodNumber = e.PeriodNumber,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsClosed = false
        };
        _db.FinancialPeriods.Add(period);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(FinancialPeriodClosedEvent e, CancellationToken ct)
    {
        var period = await _db.FinancialPeriods.FindAsync(new object[] { e.PeriodId }, ct);
        if (period != null)
        {
            period.IsClosed = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(FinancialPeriodReopenedEvent e, CancellationToken ct)
    {
        var period = await _db.FinancialPeriods.FindAsync(new object[] { e.PeriodId }, ct);
        if (period != null)
        {
            period.IsClosed = false;
            await _db.SaveChangesAsync(ct);
        }
    }
}
