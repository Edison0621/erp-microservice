using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Value Objects ---

public record ShippingAddress(string Receiver, string Phone, string Address, bool IsDefault);

public record CreditInfo(decimal Limit, int PeriodDays, decimal CurrentArrears);

// --- Events ---

public record CustomerCreatedEvent(
    Guid CustomerId, 
    string CustomerCode, 
    string CustomerName, 
    CustomerType CustomerType
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CustomerCreditUpdatedEvent(Guid CustomerId, decimal Limit, int PeriodDays) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CustomerAddressesUpdatedEvent(Guid CustomerId, List<ShippingAddress> Addresses) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Enums ---

public enum CustomerType
{
    Individual = 1,
    Enterprise
}

// --- Aggregate ---

public class Customer : AggregateRoot<Guid>
{
    public string CustomerCode { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public CustomerType CustomerType { get; private set; }
    
    public decimal CreditLimit { get; private set; }
    public int CreditPeriodDays { get; private set; }
    
    private readonly List<ShippingAddress> _addresses = [];
    public IReadOnlyCollection<ShippingAddress> Addresses => this._addresses.AsReadOnly();

    public static Customer Create(Guid id, string code, string name, CustomerType type)
    {
        Customer customer = new Customer();
        customer.ApplyChange(new CustomerCreatedEvent(id, code, name, type));
        return customer;
    }

    public void UpdateCredit(decimal limit, int periodDays)
    {
        this.ApplyChange(new CustomerCreditUpdatedEvent(this.Id, limit, periodDays));
    }

    public void UpdateAddresses(List<ShippingAddress> addresses)
    {
        this.ApplyChange(new CustomerAddressesUpdatedEvent(this.Id, addresses));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreatedEvent e:
                this.Id = e.CustomerId;
                this.CustomerCode = e.CustomerCode;
                this.CustomerName = e.CustomerName;
                this.CustomerType = e.CustomerType;
                break;
            case CustomerCreditUpdatedEvent e:
                this.CreditLimit = e.Limit;
                this.CreditPeriodDays = e.PeriodDays;
                break;
            case CustomerAddressesUpdatedEvent e:
                this._addresses.Clear();
                this._addresses.AddRange(e.Addresses);
                break;
        }
    }
}
