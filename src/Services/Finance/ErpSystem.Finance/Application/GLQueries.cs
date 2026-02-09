using MediatR;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Application;

// --- Queries ---
public record GetChartOfAccountsQuery : IRequest<List<AccountReadModel>>;

public record GetJournalEntryQuery(Guid JournalEntryId) : IRequest<JournalEntryDetailDto?>;

public record GetTrialBalanceQuery(DateTime? AsOfDate) : IRequest<List<TrialBalanceLineDto>>;

public record JournalEntryDetailDto(JournalEntryReadModel Header, List<JournalEntryLineReadModel> Lines);

public record TrialBalanceLineDto(string AccountCode, string AccountName, decimal Debit, decimal Credit);

// --- Handler ---
public class GlQueryHandler(FinanceReadDbContext db) :
    IRequestHandler<GetChartOfAccountsQuery, List<AccountReadModel>>,
    IRequestHandler<GetJournalEntryQuery, JournalEntryDetailDto?>,
    IRequestHandler<GetTrialBalanceQuery, List<TrialBalanceLineDto>>
{
    public async Task<List<AccountReadModel>> Handle(GetChartOfAccountsQuery request, CancellationToken ct)
    {
        return await db.Accounts.OrderBy(a => a.Code).ToListAsync(ct);
    }

    public async Task<JournalEntryDetailDto?> Handle(GetJournalEntryQuery request, CancellationToken ct)
    {
        JournalEntryReadModel? header = await db.JournalEntries.FindAsync([request.JournalEntryId], ct);
        if (header == null) return null;

        List<JournalEntryLineReadModel> lines = await db.JournalEntryLines.Where(l => l.JournalEntryId == request.JournalEntryId).ToListAsync(ct);
        return new JournalEntryDetailDto(header, lines);
    }

    public async Task<List<TrialBalanceLineDto>> Handle(GetTrialBalanceQuery request, CancellationToken ct)
    {
        // Simple Trial Balance calculation on the fly
        // In production, this should use pre-calculated balances or be optimized
        
        DateTime date = request.AsOfDate ?? DateTime.UtcNow;

        // Get all posted JE lines up to date
        // Join with JournalEntries to filter by Date and Status=Posted
        var lines = await (from l in db.JournalEntryLines
                           join h in db.JournalEntries on l.JournalEntryId equals h.JournalEntryId
                           where h.Status == 1 // Posted
                           && h.PostingDate <= date
                           select new { l.AccountId, l.Debit, l.Credit })
                          .ToListAsync(ct);

        var grouped = lines.GroupBy(l => l.AccountId)
                           .Select(g => new 
                           { 
                               AccountId = g.Key, 
                               TotalDebit = g.Sum(x => x.Debit), 
                               TotalCredit = g.Sum(x => x.Credit) 
                           })
                           .ToList();

        Dictionary<Guid, AccountReadModel> accounts = await db.Accounts.ToDictionaryAsync(a => a.AccountId, ct);

        List<TrialBalanceLineDto> result = [];
        foreach (var g in grouped)
        {
            if (accounts.TryGetValue(g.AccountId, out AccountReadModel? account))
            {
                // Verify Balance logic: 
                // For TB, we usually show Net Debit or Net Credit, or both.
                // Let's show both totals for now.
                result.Add(new TrialBalanceLineDto(account.Code, account.Name, g.TotalDebit, g.TotalCredit));
            }
        }

        return result.OrderBy(x => x.AccountCode).ToList();
    }
}
