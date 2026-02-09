using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

// --- Enums ---
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum AccountClass
{
    Current = 1,
    NonCurrent = 2
}

public enum BalanceType
{
    Debit = 1,
    Credit = 2
}

// --- Events ---
public record AccountCreatedEvent(Guid AccountId, string Code, string Name, AccountType Type, AccountClass Class, Guid? ParentAccountId, BalanceType BalanceType, string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AccountDetailsUpdatedEvent(Guid AccountId, string Name, bool IsActive) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate Root ---
public class Account : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public AccountClass Class { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public BalanceType BalanceType { get; private set; }
    public bool IsActive { get; private set; }
    public string Currency { get; private set; } = "CNY";

    // Constructor for creating new account
    public static Account Create(Guid id, string code, string name, AccountType type, AccountClass accountClass, Guid? parentId, BalanceType balanceType, string currency)
    {
        Account account = new Account();
        account.ApplyChange(new AccountCreatedEvent(id, code, name, type, accountClass, parentId, balanceType, currency));
        return account;
    }

    public void UpdateDetails(string name, bool isActive)
    {
        this.ApplyChange(new AccountDetailsUpdatedEvent(this.Id, name, isActive));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreatedEvent e:
                this.Id = e.AccountId;
                this.Code = e.Code;
                this.Name = e.Name;
                this.Type = e.Type;
                this.Class = e.Class;
                this.ParentAccountId = e.ParentAccountId;
                this.BalanceType = e.BalanceType;
                this.Currency = e.Currency;
                this.IsActive = true;
                break;
            case AccountDetailsUpdatedEvent e:
                this.Name = e.Name;
                this.IsActive = e.IsActive;
                break;
        }
    }
}
