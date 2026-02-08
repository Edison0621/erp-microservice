using MediatR;
using ErpSystem.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Inventory.Infrastructure;

public class InventoryProjections : 
    INotificationHandler<InventoryItemCreatedEvent>,
    INotificationHandler<StockReceivedEvent>,
    INotificationHandler<StockIssuedEvent>,
    INotificationHandler<StockReservedEvent>,
    INotificationHandler<ReservationReleasedEvent>,
    INotificationHandler<StockAdjustedEvent>,
    INotificationHandler<StockTransferredEvent>
{
    private readonly InventoryReadDbContext _readDb;

    public InventoryProjections(InventoryReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task Handle(InventoryItemCreatedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        if (item == null)
        {
            item = new InventoryItemReadModel
            {
                Id = n.InventoryItemId,
                WarehouseId = n.WarehouseId,
                BinId = n.BinId,
                MaterialId = n.MaterialId,
                OnHandQuantity = 0,
                ReservedQuantity = 0,
                AvailableQuantity = 0,
                UnitCost = 0,
                TotalValue = 0,
                LastMovementAt = n.OccurredOn
            };
            _readDb.InventoryItems.Add(item);
            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(StockReceivedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems
            .FirstOrDefaultAsync(x => x.WarehouseId == n.WarehouseId && x.BinId == n.BinId && x.MaterialId == n.MaterialId, ct);

        if (item == null)
        {
            item = new InventoryItemReadModel
            {
                Id = n.InventoryItemId,
                WarehouseId = n.WarehouseId,
                BinId = n.BinId,
                MaterialId = n.MaterialId,
                OnHandQuantity = 0,
                ReservedQuantity = 0,
                AvailableQuantity = 0,
                UnitCost = n.UnitCost,
                TotalValue = 0
            };
            _readDb.InventoryItems.Add(item);
        }

        item.OnHandQuantity += n.Quantity;
        item.UnitCost = n.UnitCost; // Simplified: last cost wins for read model display
        item.TotalValue = item.OnHandQuantity * item.UnitCost;
        item.AvailableQuantity = item.OnHandQuantity - item.ReservedQuantity;
        item.LastMovementAt = n.OccurredOn;

        _readDb.StockTransactions.Add(new StockTransactionReadModel
        {
            Id = Guid.NewGuid(),
            InventoryItemId = item.Id,
            WarehouseId = n.WarehouseId,
            MaterialId = n.MaterialId,
            SourceType = n.SourceType,
            SourceId = n.SourceId,
            QuantityChange = n.Quantity,
            PerformedBy = n.PerformedBy,
            OccurredOn = n.OccurredOn
        });

        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(StockIssuedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        if (item != null)
        {
            item.OnHandQuantity -= n.Quantity;
            item.AvailableQuantity = item.OnHandQuantity - item.ReservedQuantity;
            item.TotalValue -= n.CostAmount;
            if (item.OnHandQuantity > 0)
            {
                item.UnitCost = item.TotalValue / item.OnHandQuantity;
            }
            else
            {
                item.UnitCost = 0;
                item.TotalValue = 0;
            }
            item.LastMovementAt = n.OccurredOn;

            _readDb.StockTransactions.Add(new StockTransactionReadModel
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                WarehouseId = item.WarehouseId,
                MaterialId = item.MaterialId,
                SourceType = n.SourceType,
                SourceId = n.SourceId,
                QuantityChange = -n.Quantity,
                PerformedBy = n.PerformedBy,
                OccurredOn = n.OccurredOn
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(StockReservedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        if (item != null)
        {
            item.ReservedQuantity += n.Quantity;
            item.AvailableQuantity = item.OnHandQuantity - item.ReservedQuantity;

            _readDb.StockReservations.Add(new StockReservationReadModel
            {
                Id = n.ReservationId,
                InventoryItemId = n.InventoryItemId,
                SourceType = n.SourceType,
                SourceId = n.SourceId,
                Quantity = n.Quantity,
                ReservedAt = n.OccurredOn,
                ExpiryDate = n.ExpiryDate,
                IsReleased = false
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ReservationReleasedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        var reservation = await _readDb.StockReservations.FindAsync(new object[] { n.ReservationId }, ct);
        
        if (item != null && reservation != null && !reservation.IsReleased)
        {
            item.ReservedQuantity -= n.Quantity;
            item.AvailableQuantity = item.OnHandQuantity - item.ReservedQuantity;
            reservation.IsReleased = true;

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(StockAdjustedEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        if (item != null)
        {
            item.OnHandQuantity = n.NewQuantity;
            item.AvailableQuantity = item.OnHandQuantity - item.ReservedQuantity;
            item.LastMovementAt = n.OccurredOn;

            _readDb.StockTransactions.Add(new StockTransactionReadModel
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                WarehouseId = item.WarehouseId,
                MaterialId = item.MaterialId,
                SourceType = "ADJUSTMENT",
                SourceId = n.Reason,
                QuantityChange = n.Difference,
                PerformedBy = n.PerformedBy,
                OccurredOn = n.OccurredOn
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(StockTransferredEvent n, CancellationToken ct)
    {
        var item = await _readDb.InventoryItems.FindAsync(new object[] { n.InventoryItemId }, ct);
        if (item != null)
        {
            item.WarehouseId = n.ToWarehouseId;
            item.BinId = n.ToBinId;
            item.LastMovementAt = n.OccurredOn;

            _readDb.StockTransactions.Add(new StockTransactionReadModel
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                WarehouseId = n.ToWarehouseId,
                MaterialId = item.MaterialId,
                SourceType = "TRANSFER",
                SourceId = $"{n.FromWarehouseId}/{n.FromBinId} -> {n.ToWarehouseId}/{n.ToBinId}",
                QuantityChange = 0, // Location change only in this aggregate model
                PerformedBy = n.PerformedBy,
                OccurredOn = n.OccurredOn
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }
}
