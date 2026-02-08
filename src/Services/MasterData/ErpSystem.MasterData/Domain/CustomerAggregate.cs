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
    
    private readonly List<ShippingAddress> _addresses = new();
    public IReadOnlyCollection<ShippingAddress> Addresses => _addresses.AsReadOnly();

    public static Customer Create(Guid id, string code, string name, CustomerType type)
    {
        var customer = new Customer();
        customer.ApplyChange(new CustomerCreatedEvent(id, code, name, type));
        return customer;
    }

    public void UpdateCredit(decimal limit, int periodDays)
    {
        ApplyChange(new CustomerCreditUpdatedEvent(Id, limit, periodDays));
    }

    public void UpdateAddresses(List<ShippingAddress> addresses)
    {
        ApplyChange(new CustomerAddressesUpdatedEvent(Id, addresses));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreatedEvent e:
                Id = e.CustomerId;
                CustomerCode = e.CustomerCode;
                CustomerName = e.CustomerName;
                CustomerType = e.CustomerType;
                break;
            case CustomerCreditUpdatedEvent e:
                CreditLimit = e.Limit;
                CreditPeriodDays = e.PeriodDays;
                break;
            case CustomerAddressesUpdatedEvent e:
                _addresses.Clear();
                _addresses.AddRange(e.Addresses);
                break;
        }
    }
}
