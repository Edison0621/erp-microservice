using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Value Objects ---

public record ContactPerson(string Name, string Position, string Phone, string Email, bool IsPrimary);

public record BankAccount(string BankName, string AccountNumber, string AccountName, bool IsDefault);

// --- Events ---

public record SupplierCreatedEvent(
    Guid SupplierId, 
    string SupplierCode, 
    string SupplierName, 
    SupplierType SupplierType,
    string CreditCode
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SupplierProfileUpdatedEvent(
    Guid SupplierId, 
    List<ContactPerson> Contacts, 
    List<BankAccount> BankAccounts
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SupplierStatusChangedEvent(Guid SupplierId, bool IsBlacklisted, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SupplierLevelChangedEvent(Guid SupplierId, SupplierLevel NewLevel) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Enums ---

public enum SupplierType
{
    RawMaterial = 1,
    Outsourcing,
    Service
}

public enum SupplierLevel
{
    A = 1,
    B,
    C,
    D
}

// --- Aggregate ---

public class Supplier : AggregateRoot<Guid>
{
    public string SupplierCode { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public SupplierType SupplierType { get; private set; }
    public string CreditCode { get; private set; } = string.Empty;
    public SupplierLevel Level { get; private set; }
    public bool IsBlacklisted { get; private set; }

    private readonly List<ContactPerson> _contacts = [];
    public IReadOnlyCollection<ContactPerson> Contacts => this._contacts.AsReadOnly();

    private readonly List<BankAccount> _bankAccounts = [];
    public IReadOnlyCollection<BankAccount> BankAccounts => this._bankAccounts.AsReadOnly();

    public static Supplier Create(Guid id, string code, string name, SupplierType type, string creditCode)
    {
        Supplier supplier = new();
        supplier.ApplyChange(new SupplierCreatedEvent(id, code, name, type, creditCode));
        return supplier;
    }

    public void UpdateProfile(List<ContactPerson> contacts, List<BankAccount> bankAccounts)
    {
        this.ApplyChange(new SupplierProfileUpdatedEvent(this.Id, contacts, bankAccounts));
    }

    public void SetBlacklisted(bool blacklisted, string reason)
    {
        this.ApplyChange(new SupplierStatusChangedEvent(this.Id, blacklisted, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SupplierCreatedEvent e:
                this.Id = e.SupplierId;
                this.SupplierCode = e.SupplierCode;
                this.SupplierName = e.SupplierName;
                this.SupplierType = e.SupplierType;
                this.CreditCode = e.CreditCode;
                this.Level = SupplierLevel.D;
                break;
            case SupplierProfileUpdatedEvent e:
                this._contacts.Clear();
                this._contacts.AddRange(e.Contacts);
                this._bankAccounts.Clear();
                this._bankAccounts.AddRange(e.BankAccounts);
                break;
            case SupplierStatusChangedEvent e:
                this.IsBlacklisted = e.IsBlacklisted;
                break;
            case SupplierLevelChangedEvent e:
                this.Level = e.NewLevel;
                break;
        }
    }
}
