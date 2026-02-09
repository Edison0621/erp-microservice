using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Application;

// --- Material Commands ---

public record CreateMaterialRequest(
    string Name, 
    MaterialType Type, 
    string UoM, 
    Guid CategoryId, 
    CostDetail InitialCost);

public record UpdateMaterialInfoCommand(
    Guid MaterialId, 
    string Name, 
    string Description, 
    string Specification, 
    string Brand, 
    string Manufacturer) : IRequest;

public record UpdateMaterialAttributesCommand(Guid MaterialId, List<MaterialAttribute> Attributes) : IRequest;

// --- Partner Commands ---

public record CreateSupplierRequest(string Name, SupplierType Type, string CreditCode);

public record UpdateSupplierProfileCommand(Guid SupplierId, List<ContactPerson> Contacts, List<BankAccount> BankAccounts) : IRequest;

public record CreateCustomerRequest(string Name, CustomerType Type);

public record UpdateCustomerAddressesCommand(Guid CustomerId, List<ShippingAddress> Addresses) : IRequest;

// --- Tree Commands ---

public record CreateCategoryCommand(string Name, Guid? ParentId, int Level) : IRequest<Guid>;

public record CreateLocationCommand(Guid WarehouseId, string Name, string Type) : IRequest<Guid>;

// --- Handlers ---

public class MasterDataCommandHandler(
    EventStoreRepository<Material> materialRepo,
    EventStoreRepository<Supplier> supplierRepo,
    EventStoreRepository<Customer> customerRepo,
    EventStoreRepository<MaterialCategory> categoryRepo,
    EventStoreRepository<WarehouseLocation> locationRepo,
    ICodeGenerator codeGen)
    :
        IRequestHandler<UpdateMaterialInfoCommand>,
        IRequestHandler<UpdateMaterialAttributesCommand>,
        IRequestHandler<UpdateSupplierProfileCommand>,
        IRequestHandler<UpdateCustomerAddressesCommand>,
        IRequestHandler<CreateCategoryCommand, Guid>,
        IRequestHandler<CreateLocationCommand, Guid>
{
    private readonly ICodeGenerator _codeGen = codeGen;

    // Material
    public async Task Handle(UpdateMaterialInfoCommand r, CancellationToken ct)
    {
        Material? m = await materialRepo.LoadAsync(r.MaterialId);
        m?.UpdateInfo(r.Name, r.Description, r.Specification, r.Brand, r.Manufacturer);
        if (m != null) await materialRepo.SaveAsync(m);
    }

    public async Task Handle(UpdateMaterialAttributesCommand r, CancellationToken ct)
    {
        Material? m = await materialRepo.LoadAsync(r.MaterialId);
        m?.UpdateAttributes(r.Attributes);
        if (m != null) await materialRepo.SaveAsync(m);
    }

    // Partner
    public async Task Handle(UpdateSupplierProfileCommand r, CancellationToken ct)
    {
        Supplier? s = await supplierRepo.LoadAsync(r.SupplierId);
        s?.UpdateProfile(r.Contacts, r.BankAccounts);
        if (s != null) await supplierRepo.SaveAsync(s);
    }

    public async Task Handle(UpdateCustomerAddressesCommand r, CancellationToken ct)
    {
        Customer? c = await customerRepo.LoadAsync(r.CustomerId);
        c?.UpdateAddresses(r.Addresses);
        if (c != null) await customerRepo.SaveAsync(c);
    }

    // Category / Location
    public async Task<Guid> Handle(CreateCategoryCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string code = "CAT-" + Guid.NewGuid().ToString()[..4]; // Simplified cat code
        MaterialCategory cat = MaterialCategory.Create(id, code, r.Name, r.ParentId, r.Level);
        await categoryRepo.SaveAsync(cat);
        return id;
    }

    public async Task<Guid> Handle(CreateLocationCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string code = "LOC-" + Guid.NewGuid().ToString()[..4];
        WarehouseLocation loc = WarehouseLocation.Create(id, r.WarehouseId, code, r.Name, r.Type);
        await locationRepo.SaveAsync(loc);
        return id;
    }
}
