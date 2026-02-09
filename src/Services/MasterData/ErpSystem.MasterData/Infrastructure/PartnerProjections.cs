using System.Text.Json;
using ErpSystem.MasterData.Domain;
using MediatR;

namespace ErpSystem.MasterData.Infrastructure;

public class PartnerProjections(MasterDataReadDbContext dbContext) :
    INotificationHandler<SupplierCreatedEvent>,
    INotificationHandler<SupplierProfileUpdatedEvent>,
    INotificationHandler<SupplierStatusChangedEvent>,
    INotificationHandler<CustomerCreatedEvent>,
    INotificationHandler<CustomerCreditUpdatedEvent>,
    INotificationHandler<CustomerAddressesUpdatedEvent>
{
    public async Task Handle(SupplierCreatedEvent n, CancellationToken ct)
    {
        dbContext.Suppliers.Add(new SupplierReadModel 
        {
            SupplierId = n.SupplierId,
            SupplierCode = n.SupplierCode,
            SupplierName = n.SupplierName,
            SupplierType = n.SupplierType.ToString()
        });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(SupplierProfileUpdatedEvent n, CancellationToken ct)
    {
        SupplierReadModel? s = await dbContext.Suppliers.FindAsync([n.SupplierId], ct);
        if (s != null)
        {
            s.Contacts = JsonSerializer.Serialize(n.Contacts);
            s.BankAccounts = JsonSerializer.Serialize(n.BankAccounts);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(SupplierStatusChangedEvent n, CancellationToken ct)
    {
        SupplierReadModel? s = await dbContext.Suppliers.FindAsync([n.SupplierId], ct);
        if (s != null)
        {
            s.IsBlacklisted = n.IsBlacklisted;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CustomerCreatedEvent n, CancellationToken ct)
    {
        dbContext.Customers.Add(new CustomerReadModel
        {
            CustomerId = n.CustomerId,
            CustomerCode = n.CustomerCode,
            CustomerName = n.CustomerName,
            Type = n.CustomerType.ToString()
        });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(CustomerCreditUpdatedEvent n, CancellationToken ct)
    {
        CustomerReadModel? c = await dbContext.Customers.FindAsync([n.CustomerId], ct);
        if (c != null)
        {
            c.CreditLimit = n.Limit;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CustomerAddressesUpdatedEvent n, CancellationToken ct)
    {
        CustomerReadModel? c = await dbContext.Customers.FindAsync([n.CustomerId], ct);
        if (c != null)
        {
            c.Addresses = JsonSerializer.Serialize(n.Addresses);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
