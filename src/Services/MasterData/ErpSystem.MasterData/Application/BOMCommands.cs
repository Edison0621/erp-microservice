using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.MasterData.Infrastructure;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Application;

// --- BOM Commands ---

public record CreateBOMRequest(
    Guid ParentMaterialId, 
    string Name, 
    string Version, 
    DateTime EffectiveDate);

public record AddBOMComponentCommand(
    Guid BOMId, 
    Guid MaterialId, 
    decimal Quantity, 
    string? Note) : IRequest;

public record ActivateBOMCommand(Guid BOMId) : IRequest;

// --- Handlers ---

public class BOMCommandHandler : 
    IRequestHandler<AddBOMComponentCommand>,
    IRequestHandler<ActivateBOMCommand>
{
    private readonly EventStoreRepository<BillOfMaterials> _bomRepo;

    public BOMCommandHandler(EventStoreRepository<BillOfMaterials> bomRepo)
    {
        _bomRepo = bomRepo;
    }

    public async Task Handle(AddBOMComponentCommand r, CancellationToken ct)
    {
        var bom = await _bomRepo.LoadAsync(r.BOMId);
        if (bom == null) throw new KeyNotFoundException($"BOM with ID {r.BOMId} not found.");

        bom.AddComponent(r.MaterialId, r.Quantity, r.Note);
        await _bomRepo.SaveAsync(bom);
    }

    public async Task Handle(ActivateBOMCommand r, CancellationToken ct)
    {
        var bom = await _bomRepo.LoadAsync(r.BOMId);
        if (bom == null) throw new KeyNotFoundException($"BOM with ID {r.BOMId} not found.");

        bom.Activate();
        await _bomRepo.SaveAsync(bom);
    }
}
