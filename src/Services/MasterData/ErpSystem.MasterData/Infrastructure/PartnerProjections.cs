using System.Text.Json;
using ErpSystem.MasterData.Domain;
using MediatR;

namespace ErpSystem.MasterData.Infrastructure;

public class PartnerProjections : 
    INotificationHandler<SupplierCreatedEvent>,
    INotificationHandler<SupplierProfileUpdatedEvent>,
    INotificationHandler<SupplierStatusChangedEvent>,
    INotificationHandler<CustomerCreatedEvent>,
    INotificationHandler<CustomerCreditUpdatedEvent>,
    INotificationHandler<CustomerAddressesUpdatedEvent>
{
    private readonly MasterDataReadDbContext _dbContext;

    public PartnerProjections(MasterDataReadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(SupplierCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Suppliers.Add(new SupplierReadModel 
        {
            SupplierId = n.SupplierId,
            SupplierCode = n.SupplierCode,
            SupplierName = n.SupplierName,
            SupplierType = n.SupplierType.ToString()
        });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(SupplierProfileUpdatedEvent n, CancellationToken ct)
    {
        var s = await _dbContext.Suppliers.FindAsync(new object[] { n.SupplierId }, ct);
        if (s != null)
        {
            s.Contacts = JsonSerializer.Serialize(n.Contacts);
            s.BankAccounts = JsonSerializer.Serialize(n.BankAccounts);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(SupplierStatusChangedEvent n, CancellationToken ct)
    {
        var s = await _dbContext.Suppliers.FindAsync(new object[] { n.SupplierId }, ct);
        if (s != null)
        {
            s.IsBlacklisted = n.IsBlacklisted;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CustomerCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Customers.Add(new CustomerReadModel
        {
            CustomerId = n.CustomerId,
            CustomerCode = n.CustomerCode,
            CustomerName = n.CustomerName,
            Type = n.CustomerType.ToString()
        });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(CustomerCreditUpdatedEvent n, CancellationToken ct)
    {
        var c = await _dbContext.Customers.FindAsync(new object[] { n.CustomerId }, ct);
        if (c != null)
        {
            c.CreditLimit = n.Limit;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CustomerAddressesUpdatedEvent n, CancellationToken ct)
    {
        var c = await _dbContext.Customers.FindAsync(new object[] { n.CustomerId }, ct);
        if (c != null)
        {
            c.Addresses = JsonSerializer.Serialize(n.Addresses);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
