using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.MasterData.Infrastructure;
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

public class MasterDataCommandHandler : 
    IRequestHandler<UpdateMaterialInfoCommand>,
    IRequestHandler<UpdateMaterialAttributesCommand>,
    IRequestHandler<UpdateSupplierProfileCommand>,
    IRequestHandler<UpdateCustomerAddressesCommand>,
    IRequestHandler<CreateCategoryCommand, Guid>,
    IRequestHandler<CreateLocationCommand, Guid>
{
    private readonly EventStoreRepository<Material> _materialRepo;
    private readonly EventStoreRepository<Supplier> _supplierRepo;
    private readonly EventStoreRepository<Customer> _customerRepo;
    private readonly EventStoreRepository<MaterialCategory> _categoryRepo;
    private readonly EventStoreRepository<WarehouseLocation> _locationRepo;
    private readonly ICodeGenerator _codeGen;

    public MasterDataCommandHandler(
        EventStoreRepository<Material> materialRepo,
        EventStoreRepository<Supplier> supplierRepo,
        EventStoreRepository<Customer> customerRepo,
        EventStoreRepository<MaterialCategory> categoryRepo,
        EventStoreRepository<WarehouseLocation> locationRepo,
        ICodeGenerator codeGen)
    {
        _materialRepo = materialRepo;
        _supplierRepo = supplierRepo;
        _customerRepo = customerRepo;
        _categoryRepo = categoryRepo;
        _locationRepo = locationRepo;
        _codeGen = codeGen;
    }

    // Material
    public async Task Handle(UpdateMaterialInfoCommand r, CancellationToken ct)
    {
        var m = await _materialRepo.LoadAsync(r.MaterialId);
        m?.UpdateInfo(r.Name, r.Description, r.Specification, r.Brand, r.Manufacturer);
        if (m != null) await _materialRepo.SaveAsync(m);
    }

    public async Task Handle(UpdateMaterialAttributesCommand r, CancellationToken ct)
    {
        var m = await _materialRepo.LoadAsync(r.MaterialId);
        m?.UpdateAttributes(r.Attributes);
        if (m != null) await _materialRepo.SaveAsync(m);
    }

    // Partner
    public async Task Handle(UpdateSupplierProfileCommand r, CancellationToken ct)
    {
        var s = await _supplierRepo.LoadAsync(r.SupplierId);
        s?.UpdateProfile(r.Contacts, r.BankAccounts);
        if (s != null) await _supplierRepo.SaveAsync(s);
    }

    public async Task Handle(UpdateCustomerAddressesCommand r, CancellationToken ct)
    {
        var c = await _customerRepo.LoadAsync(r.CustomerId);
        c?.UpdateAddresses(r.Addresses);
        if (c != null) await _customerRepo.SaveAsync(c);
    }

    // Category / Location
    public async Task<Guid> Handle(CreateCategoryCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var code = "CAT-" + Guid.NewGuid().ToString()[..4]; // Simplified cat code
        var cat = MaterialCategory.Create(id, code, r.Name, r.ParentId, r.Level);
        await _categoryRepo.SaveAsync(cat);
        return id;
    }

    public async Task<Guid> Handle(CreateLocationCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var code = "LOC-" + Guid.NewGuid().ToString()[..4];
        var loc = WarehouseLocation.Create(id, r.WarehouseId, code, r.Name, r.Type);
        await _locationRepo.SaveAsync(loc);
        return id;
    }
}
