using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Inventory.Domain;
using ErpSystem.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Inventory.Application;

public record ReceiveStockCommand(
    string WarehouseId,
    string BinId,
    string MaterialId,
    decimal Quantity,
    decimal UnitCost,
    string SourceType,
    string SourceId,
    string PerformedBy
) : IRequest<Guid>;

public record TransferStockCommand(
    Guid InventoryItemId,
    string ToWarehouseId,
    string ToBinId,
    decimal Quantity,
    string Reason,
    string PerformedBy
) : IRequest<bool>;

public record IssueStockCommand(
    Guid InventoryItemId,
    decimal Quantity,
    string SourceType,
    string SourceId,
    string PerformedBy,
    Guid? RelatedReservationId = null
) : IRequest<bool>;

public record ReserveStockCommand(
    Guid InventoryItemId,
    decimal Quantity,
    string SourceType,
    string SourceId,
    DateTime? ExpiryDate
) : IRequest<Guid>;

public record ReleaseReservationCommand(
    Guid InventoryItemId,
    Guid ReservationId,
    decimal Quantity,
    string Reason
) : IRequest<bool>;

public record AdjustStockCommand(
    Guid InventoryItemId,
    decimal NewQuantity,
    string Reason,
    string PerformedBy
) : IRequest<bool>;

public class InventoryCommandHandler : 
    IRequestHandler<ReceiveStockCommand, Guid>,
    IRequestHandler<IssueStockCommand, bool>,
    IRequestHandler<ReserveStockCommand, Guid>,
    IRequestHandler<ReleaseReservationCommand, bool>,
    IRequestHandler<AdjustStockCommand, bool>,
    IRequestHandler<TransferStockCommand, bool>
{
    private readonly EventStoreRepository<InventoryItem> _repo;
    private readonly InventoryReadDbContext _readDb;

    public InventoryCommandHandler(EventStoreRepository<InventoryItem> repo, InventoryReadDbContext readDb)
    {
        _repo = repo;
        _readDb = readDb;
    }

    public async Task<Guid> Handle(ReceiveStockCommand request, CancellationToken ct)
    {
        // Find existing or start a new one
        var itemModel = await _readDb.InventoryItems
            .FirstOrDefaultAsync(x => x.WarehouseId == request.WarehouseId && x.BinId == request.BinId && x.MaterialId == request.MaterialId, ct);
        
        InventoryItem item;
        if (itemModel == null)
        {
            var id = Guid.NewGuid();
            item = InventoryItem.Create(id, request.WarehouseId, request.BinId, request.MaterialId);
            item.ReceiveStock(request.Quantity, request.UnitCost, request.SourceType, request.SourceId, request.PerformedBy);
        }
        else
        {
            item = await _repo.LoadAsync(itemModel.Id) ?? throw new Exception("InventoryItem unexpectedly missing from store");
            item.ReceiveStock(request.Quantity, request.UnitCost, request.SourceType, request.SourceId, request.PerformedBy);
        }

        await _repo.SaveAsync(item);
        return item.Id;
    }

    public async Task<bool> Handle(TransferStockCommand request, CancellationToken ct)
    {
        var item = await _repo.LoadAsync(request.InventoryItemId);
        if (item == null) throw new KeyNotFoundException("Inventory item not found");

        item.TransferTo(request.ToWarehouseId, request.ToBinId, request.Quantity, request.Reason, request.PerformedBy);
        await _repo.SaveAsync(item);
        return true;
    }

    public async Task<bool> Handle(IssueStockCommand request, CancellationToken ct)
    {
        var item = await _repo.LoadAsync(request.InventoryItemId);
        if (item == null) throw new KeyNotFoundException("Inventory item not found");
        
        item.IssueStock(request.Quantity, request.SourceType, request.SourceId, request.PerformedBy, request.RelatedReservationId);
        await _repo.SaveAsync(item);
        return true;
    }

    public async Task<Guid> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        var item = await _repo.LoadAsync(request.InventoryItemId);
        if (item == null) throw new KeyNotFoundException("Inventory item not found");

        var reservationId = Guid.NewGuid();
        item.ReserveStock(reservationId, request.Quantity, request.SourceType, request.SourceId, request.ExpiryDate);
        await _repo.SaveAsync(item);
        return reservationId;
    }

    public async Task<bool> Handle(ReleaseReservationCommand request, CancellationToken ct)
    {
        var item = await _repo.LoadAsync(request.InventoryItemId);
        if (item == null) throw new KeyNotFoundException("Inventory item not found");

        item.ReleaseReservation(request.ReservationId, request.Quantity, request.Reason);
        await _repo.SaveAsync(item);
        return true;
    }

    public async Task<bool> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var item = await _repo.LoadAsync(request.InventoryItemId);
        if (item == null) throw new KeyNotFoundException("Inventory item not found");

        item.AdjustStock(request.NewQuantity, request.Reason, request.PerformedBy);
        await _repo.SaveAsync(item);
        return true;
    }
}
