using MediatR;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Application;

// --- Queries ---
public record GetChartOfAccountsQuery() : IRequest<List<AccountReadModel>>;
public record GetJournalEntryQuery(Guid JournalEntryId) : IRequest<JournalEntryDetailDto?>;
public record GetTrialBalanceQuery(DateTime? AsOfDate) : IRequest<List<TrialBalanceLineDto>>;

public record JournalEntryDetailDto(JournalEntryReadModel Header, List<JournalEntryLineReadModel> Lines);
public record TrialBalanceLineDto(string AccountCode, string AccountName, decimal Debit, decimal Credit);

// --- Handler ---
public class GLQueryHandler : 
    IRequestHandler<GetChartOfAccountsQuery, List<AccountReadModel>>,
    IRequestHandler<GetJournalEntryQuery, JournalEntryDetailDto?>,
    IRequestHandler<GetTrialBalanceQuery, List<TrialBalanceLineDto>>
{
    private readonly FinanceReadDbContext _db;

    public GLQueryHandler(FinanceReadDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccountReadModel>> Handle(GetChartOfAccountsQuery request, CancellationToken ct)
    {
        return await _db.Accounts.OrderBy(a => a.Code).ToListAsync(ct);
    }

    public async Task<JournalEntryDetailDto?> Handle(GetJournalEntryQuery request, CancellationToken ct)
    {
        var header = await _db.JournalEntries.FindAsync(new object[] { request.JournalEntryId }, ct);
        if (header == null) return null;

        var lines = await _db.JournalEntryLines.Where(l => l.JournalEntryId == request.JournalEntryId).ToListAsync(ct);
        return new JournalEntryDetailDto(header, lines);
    }

    public async Task<List<TrialBalanceLineDto>> Handle(GetTrialBalanceQuery request, CancellationToken ct)
    {
        // Simple Trial Balance calculation on the fly
        // In production, this should use pre-calculated balances or be optimized
        
        var date = request.AsOfDate ?? DateTime.UtcNow;

        // Get all posted JE lines up to date
        // Join with JournalEntries to filter by Date and Status=Posted
        var lines = await (from l in _db.JournalEntryLines
                           join h in _db.JournalEntries on l.JournalEntryId equals h.JournalEntryId
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

        var accounts = await _db.Accounts.ToDictionaryAsync(a => a.AccountId, ct);

        var result = new List<TrialBalanceLineDto>();
        foreach (var g in grouped)
        {
            if (accounts.TryGetValue(g.AccountId, out var account))
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
