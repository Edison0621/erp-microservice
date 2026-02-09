using MediatR;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Application;

// --- BOM Commands ---

public record CreateBomRequest(
    Guid ParentMaterialId, 
    string Name, 
    string Version, 
    DateTime EffectiveDate);

public record AddBomComponentCommand(
    Guid BomId, 
    Guid MaterialId, 
    decimal Quantity, 
    string? Note) : IRequest;

public record ActivateBomCommand(Guid BomId) : IRequest;

/// <summary>
/// Class BOMCommandHandler.
/// </summary>
/// <param name="bomRepo">The bom repo.</param>
public class BomCommandHandler(EventStoreRepository<BillOfMaterials> bomRepo) :
    IRequestHandler<AddBomComponentCommand>,
    IRequestHandler<ActivateBomCommand>
{
    public async Task Handle(AddBomComponentCommand r, CancellationToken ct)
    {
        BillOfMaterials? bom = await bomRepo.LoadAsync(r.BomId);
        if (bom == null) throw new KeyNotFoundException($"BOM with ID {r.BomId} not found.");

        bom.AddComponent(r.MaterialId, r.Quantity, r.Note);
        await bomRepo.SaveAsync(bom);
    }

    public async Task Handle(ActivateBomCommand r, CancellationToken ct)
    {
        BillOfMaterials? bom = await bomRepo.LoadAsync(r.BomId);
        if (bom == null) throw new KeyNotFoundException($"BOM with ID {r.BomId} not found.");

        bom.Activate();
        await bomRepo.SaveAsync(bom);
    }
}
