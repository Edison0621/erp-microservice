using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.MasterData.Infrastructure;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Application;

public record CreateMaterialCommand(CreateMaterialRequest Request) : IRequest<Guid>;
public record CreateSupplierCommand(CreateSupplierRequest Request) : IRequest<Guid>;
public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<Guid>;
public record CreateBOMCommand(CreateBOMRequest Request) : IRequest<Guid>;

public class CreationCommandHandler : 
    IRequestHandler<CreateMaterialCommand, Guid>,
    IRequestHandler<CreateSupplierCommand, Guid>,
    IRequestHandler<CreateCustomerCommand, Guid>,
    IRequestHandler<CreateBOMCommand, Guid>
{
    private readonly EventStoreRepository<Material> _materialRepo;
    private readonly EventStoreRepository<Supplier> _supplierRepo;
    private readonly EventStoreRepository<Customer> _customerRepo;
    private readonly EventStoreRepository<BillOfMaterials> _bomRepo;
    private readonly ICodeGenerator _codeGen;

    public CreationCommandHandler(
        EventStoreRepository<Material> materialRepo,
        EventStoreRepository<Supplier> supplierRepo,
        EventStoreRepository<Customer> customerRepo,
        EventStoreRepository<BillOfMaterials> bomRepo,
        ICodeGenerator codeGen)
    {
        _materialRepo = materialRepo;
        _supplierRepo = supplierRepo;
        _customerRepo = customerRepo;
        _bomRepo = bomRepo;
        _codeGen = codeGen;
    }

    public async Task<Guid> Handle(CreateMaterialCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var code = _codeGen.GenerateMaterialCode();
        var m = Material.Create(id, code, r.Request.Name, r.Request.Type, r.Request.UoM, r.Request.CategoryId, r.Request.InitialCost);
        await _materialRepo.SaveAsync(m);
        return id;
    }

    public async Task<Guid> Handle(CreateSupplierCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var code = _codeGen.GenerateSupplierCode();
        var s = Supplier.Create(id, code, r.Request.Name, r.Request.Type, r.Request.CreditCode);
        await _supplierRepo.SaveAsync(s);
        return id;
    }

    public async Task<Guid> Handle(CreateCustomerCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var code = _codeGen.GenerateCustomerCode();
        var c = Customer.Create(id, code, r.Request.Name, r.Request.Type);
        await _customerRepo.SaveAsync(c);
        return id;
    }

    public async Task<Guid> Handle(CreateBOMCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var bom = BillOfMaterials.Create(id, r.Request.ParentMaterialId, r.Request.Name, r.Request.Version, r.Request.EffectiveDate);
        await _bomRepo.SaveAsync(bom);
        return id;
    }
}
