using MediatR;
using ErpSystem.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Infrastructure;

public class GlProjections(FinanceReadDbContext db) :
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
    public async Task Handle(AccountCreatedEvent e, CancellationToken ct)
    {
        AccountReadModel account = new AccountReadModel
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
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(AccountDetailsUpdatedEvent e, CancellationToken ct)
    {
        AccountReadModel? account = await db.Accounts.FindAsync([e.AccountId], ct);
        if (account != null)
        {
            account.Name = e.Name;
            account.IsActive = e.IsActive;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(JournalEntryCreatedEvent e, CancellationToken ct)
    {
        JournalEntryReadModel je = new JournalEntryReadModel
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
        db.JournalEntries.Add(je);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(JournalEntryLinesUpdatedEvent e, CancellationToken ct)
    {
        // Remove existing lines
        IQueryable<JournalEntryLineReadModel> existingLines = db.JournalEntryLines.Where(l => l.JournalEntryId == e.JournalEntryId);
        db.JournalEntryLines.RemoveRange(existingLines);

        // Add new lines
        // We need Account Names for the Read Model. 
        // Ideally we fetch them, or we just store empty for now if performance is critical 
        // (and join later in Query).
        // Let's try to fetch Account Names for better Read Model usability.
        List<Guid> accountIds = e.Lines.Select(x => x.AccountId).Distinct().ToList();
        Dictionary<Guid, string> accounts = await db.Accounts.Where(a => accountIds.Contains(a.AccountId)).ToDictionaryAsync(a => a.AccountId, a => a.Name, ct);

        foreach (JournalEntryLine line in e.Lines)
        {
            string accountName = accounts.TryGetValue(line.AccountId, out string? name) ? name : "Unknown";
            db.JournalEntryLines.Add(new JournalEntryLineReadModel
            {
                JournalEntryId = e.JournalEntryId,
                AccountId = line.AccountId,
                AccountName = accountName,
                Description = line.Description,
                Debit = line.Debit,
                Credit = line.Credit
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(JournalEntryPostedEvent e, CancellationToken ct)
    {
        JournalEntryReadModel? je = await db.JournalEntries.FindAsync([e.JournalEntryId], ct);
        if (je != null)
        {
            je.Status = (int)JournalEntryStatus.Posted;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(JournalEntryVoidedEvent e, CancellationToken ct)
    {
        JournalEntryReadModel? je = await db.JournalEntries.FindAsync([e.JournalEntryId], ct);
        if (je != null)
        {
            je.Status = (int)JournalEntryStatus.Voided;
            // Should update description or status to reflect reason?
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(FinancialPeriodDefinedEvent e, CancellationToken ct)
    {
        FinancialPeriodReadModel period = new FinancialPeriodReadModel
        {
            PeriodId = e.PeriodId,
            Year = e.Year,
            PeriodNumber = e.PeriodNumber,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsClosed = false
        };
        db.FinancialPeriods.Add(period);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(FinancialPeriodClosedEvent e, CancellationToken ct)
    {
        FinancialPeriodReadModel? period = await db.FinancialPeriods.FindAsync([e.PeriodId], ct);
        if (period != null)
        {
            period.IsClosed = true;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(FinancialPeriodReopenedEvent e, CancellationToken ct)
    {
        FinancialPeriodReadModel? period = await db.FinancialPeriods.FindAsync([e.PeriodId], ct);
        if (period != null)
        {
            period.IsClosed = false;
            await db.SaveChangesAsync(ct);
        }
    }
}
