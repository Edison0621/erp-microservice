using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Finance.Domain;
using ErpSystem.Finance.Infrastructure;

namespace ErpSystem.Finance.Application;

// --- Commands ---
public record DefineAccountCommand(string Code, string Name, AccountType Type, AccountClass Class, Guid? ParentAccountId, BalanceType BalanceType, string Currency) : IRequest<Guid>;

public record UpdateAccountCommand(Guid AccountId, string Name, bool IsActive) : IRequest;

public record CreateJournalEntryCommand(string DocumentNumber, DateTime TransactionDate, DateTime PostingDate, string Description, JournalEntrySource Source, string? ReferenceNo, List<JournalEntryLineDto> Lines) : IRequest<Guid>;

public record PostJournalEntryCommand(Guid JournalEntryId) : IRequest;

public record DefineFinancialPeriodCommand(int Year, int PeriodNumber, DateTime StartDate, DateTime EndDate) : IRequest<Guid>;

public record CloseFinancialPeriodCommand(Guid PeriodId) : IRequest;

public record JournalEntryLineDto(Guid AccountId, string Description, decimal Debit, decimal Credit);

// --- Handler ---
public class GlCommandHandler(
    IEventStore eventStore,
    FinanceReadDbContext readDb) :
    IRequestHandler<DefineAccountCommand, Guid>,
    IRequestHandler<UpdateAccountCommand>,
    IRequestHandler<CreateJournalEntryCommand, Guid>,
    IRequestHandler<PostJournalEntryCommand>,
    IRequestHandler<DefineFinancialPeriodCommand, Guid>,
    IRequestHandler<CloseFinancialPeriodCommand>
{
    private readonly FinanceReadDbContext _readDb = readDb; // For validation checks if needed

    public async Task<Guid> Handle(DefineAccountCommand request, CancellationToken ct)
    {
        // Validation: Check if Code exists? (Ideally yes, but simple here)
        Guid id = Guid.NewGuid();
        Account account = Account.Create(id, request.Code, request.Name, request.Type, request.Class, request.ParentAccountId, request.BalanceType, request.Currency);
        await eventStore.SaveAggregateAsync(account);
        return id;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        Account? account = await eventStore.LoadAggregateAsync<Account>(request.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {request.AccountId} not found");

        account.UpdateDetails(request.Name, request.IsActive);
        await eventStore.SaveAggregateAsync(account);
    }

    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        JournalEntry je = JournalEntry.Create(id, request.DocumentNumber, request.TransactionDate, request.PostingDate, request.Description, request.Source, request.ReferenceNo);
        
        // Validate Accounts exist?
        // We trust the DTO for now, or check _readDb.Accounts
        
        List<JournalEntryLine> lines = request.Lines.Select(l => new JournalEntryLine(l.AccountId, "", l.Description, l.Debit, l.Credit)).ToList();

        je.UpdateLines(lines);
        await eventStore.SaveAggregateAsync(je);
        return id;
    }

    public async Task Handle(PostJournalEntryCommand request, CancellationToken ct)
    {
        JournalEntry? je = await eventStore.LoadAggregateAsync<JournalEntry>(request.JournalEntryId);
        if (je == null) throw new KeyNotFoundException($"Journal Entry {request.JournalEntryId} not found");

        // Validate Period is open
        // var period = _readDb.FinancialPeriods...
        // For MVP, just Post
        je.Post();
        await eventStore.SaveAggregateAsync(je);
    }

    public async Task<Guid> Handle(DefineFinancialPeriodCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        FinancialPeriod period = FinancialPeriod.Define(id, request.Year, request.PeriodNumber, request.StartDate, request.EndDate);
        await eventStore.SaveAggregateAsync(period);
        return id;
    }

    public async Task Handle(CloseFinancialPeriodCommand request, CancellationToken ct)
    {
        FinancialPeriod? period = await eventStore.LoadAggregateAsync<FinancialPeriod>(request.PeriodId);
        if (period == null) throw new KeyNotFoundException($"Period {request.PeriodId} not found");
        
        period.Close();
        await eventStore.SaveAggregateAsync(period);
    }
}
