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
public class GLCommandHandler : 
    IRequestHandler<DefineAccountCommand, Guid>,
    IRequestHandler<UpdateAccountCommand>,
    IRequestHandler<CreateJournalEntryCommand, Guid>,
    IRequestHandler<PostJournalEntryCommand>,
    IRequestHandler<DefineFinancialPeriodCommand, Guid>,
    IRequestHandler<CloseFinancialPeriodCommand>
{
    private readonly IEventStore _eventStore;
    private readonly FinanceReadDbContext _readDb; // For validation checks if needed

    public GLCommandHandler(
        IEventStore eventStore,
        FinanceReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    public async Task<Guid> Handle(DefineAccountCommand request, CancellationToken ct)
    {
        // Validation: Check if Code exists? (Ideally yes, but simple here)
        var id = Guid.NewGuid();
        var account = Account.Create(id, request.Code, request.Name, request.Type, request.Class, request.ParentAccountId, request.BalanceType, request.Currency);
        await _eventStore.SaveAggregateAsync(account);
        return id;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _eventStore.LoadAggregateAsync<Account>(request.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {request.AccountId} not found");

        account.UpdateDetails(request.Name, request.IsActive);
        await _eventStore.SaveAggregateAsync(account);
    }

    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var je = JournalEntry.Create(id, request.DocumentNumber, request.TransactionDate, request.PostingDate, request.Description, request.Source, request.ReferenceNo);
        
        // Validate Accounts exist?
        // We trust the DTO for now, or check _readDb.Accounts
        
        var lines = request.Lines.Select(l => 
        {
            // Ideally fetch account name here if not provided, but Aggregate handles ID.
            // ReadModel event handler will join name.
            return new JournalEntryLine(l.AccountId, "", l.Description, l.Debit, l.Credit);
        }).ToList();

        je.UpdateLines(lines);
        await _eventStore.SaveAggregateAsync(je);
        return id;
    }

    public async Task Handle(PostJournalEntryCommand request, CancellationToken ct)
    {
        var je = await _eventStore.LoadAggregateAsync<JournalEntry>(request.JournalEntryId);
        if (je == null) throw new KeyNotFoundException($"Journal Entry {request.JournalEntryId} not found");

        // Validate Period is open
        // var period = _readDb.FinancialPeriods...
        // For MVP, just Post
        je.Post();
        await _eventStore.SaveAggregateAsync(je);
    }

    public async Task<Guid> Handle(DefineFinancialPeriodCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var period = FinancialPeriod.Define(id, request.Year, request.PeriodNumber, request.StartDate, request.EndDate);
        await _eventStore.SaveAggregateAsync(period);
        return id;
    }

    public async Task Handle(CloseFinancialPeriodCommand request, CancellationToken ct)
    {
        var period = await _eventStore.LoadAggregateAsync<FinancialPeriod>(request.PeriodId);
        if (period == null) throw new KeyNotFoundException($"Period {request.PeriodId} not found");
        
        period.Close();
        await _eventStore.SaveAggregateAsync(period);
    }
}
