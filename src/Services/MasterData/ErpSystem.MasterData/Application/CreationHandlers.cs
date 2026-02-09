using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Application;

public record CreateMaterialCommand(CreateMaterialRequest Request) : IRequest<Guid>;

public record CreateSupplierCommand(CreateSupplierRequest Request) : IRequest<Guid>;

public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<Guid>;

public record CreateBomCommand(CreateBomRequest Request) : IRequest<Guid>;

public class CreationCommandHandler(
    EventStoreRepository<Material> materialRepo,
    EventStoreRepository<Supplier> supplierRepo,
    EventStoreRepository<Customer> customerRepo,
    EventStoreRepository<BillOfMaterials> bomRepo,
    ICodeGenerator codeGen)
    :
        IRequestHandler<CreateMaterialCommand, Guid>,
        IRequestHandler<CreateSupplierCommand, Guid>,
        IRequestHandler<CreateCustomerCommand, Guid>,
        IRequestHandler<CreateBomCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaterialCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string code = codeGen.GenerateMaterialCode();
        Material m = Material.Create(id, code, r.Request.Name, r.Request.Type, r.Request.UoM, r.Request.CategoryId, r.Request.InitialCost);
        await materialRepo.SaveAsync(m);
        return id;
    }

    public async Task<Guid> Handle(CreateSupplierCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string code = codeGen.GenerateSupplierCode();
        Supplier s = Supplier.Create(id, code, r.Request.Name, r.Request.Type, r.Request.CreditCode);
        await supplierRepo.SaveAsync(s);
        return id;
    }

    public async Task<Guid> Handle(CreateCustomerCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string code = codeGen.GenerateCustomerCode();
        Customer c = Customer.Create(id, code, r.Request.Name, r.Request.Type);
        await customerRepo.SaveAsync(c);
        return id;
    }

    public async Task<Guid> Handle(CreateBomCommand r, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        BillOfMaterials bom = BillOfMaterials.Create(id, r.Request.ParentMaterialId, r.Request.Name, r.Request.Version, r.Request.EffectiveDate);
        await bomRepo.SaveAsync(bom);
        return id;
    }
}
