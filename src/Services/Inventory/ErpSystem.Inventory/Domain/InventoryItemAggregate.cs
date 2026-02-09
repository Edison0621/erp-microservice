using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Inventory.Domain;

// Events
public record InventoryItemCreatedEvent(
    Guid InventoryItemId,
    string WarehouseId,
    string BinId,
    string MaterialId
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StockReceivedEvent(
    Guid InventoryItemId,
    string WarehouseId,
    string BinId,
    string MaterialId,
    decimal Quantity,
    decimal UnitCost,
    string SourceType,
    string SourceId,
    string PerformedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StockTransferredEvent(
    Guid InventoryItemId,
    string FromWarehouseId,
    string FromBinId,
    string ToWarehouseId,
    string ToBinId,
    decimal Quantity,
    string Reason,
    string PerformedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StockIssuedEvent(
    Guid InventoryItemId,
    decimal Quantity,
    decimal CostAmount,
    string SourceType,
    string SourceId,
    string PerformedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StockReservedEvent(
    Guid InventoryItemId,
    Guid ReservationId,
    decimal Quantity,
    string SourceType,
    string SourceId,
    DateTime? ExpiryDate
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ReservationReleasedEvent(
    Guid InventoryItemId,
    Guid ReservationId,
    decimal Quantity,
    string Reason
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record StockAdjustedEvent(
    Guid InventoryItemId,
    decimal NewQuantity,
    decimal Difference,
    string Reason,
    string PerformedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate Root
public class InventoryItem : AggregateRoot<Guid>
{
    public string WarehouseId { get; private set; } = string.Empty;
    public string BinId { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => this.OnHandQuantity - this.ReservedQuantity;

    // Valuation batches (FIFO)
    private readonly List<StockBatch> _batches = [];
    public IReadOnlyCollection<StockBatch> Batches => this._batches.AsReadOnly();

    public decimal TotalValue => this._batches.Sum(b => b.RemainingQuantity * b.UnitCost);

    // Track active reservations for validation
    private readonly List<Guid> _activeReservations = [];

    public static InventoryItem Create(Guid id, string warehouseId, string binId, string materialId)
    {
        InventoryItem item = new InventoryItem();
        item.ApplyChange(new InventoryItemCreatedEvent(id, warehouseId, binId, materialId));
        return item;
    }

    public void ReceiveStock(decimal quantity, decimal unitCost, string sourceType, string sourceId, string performedBy)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        this.ApplyChange(new StockReceivedEvent(this.Id, this.WarehouseId, this.BinId, this.MaterialId, quantity, unitCost, sourceType, sourceId, performedBy));
    }

    public void TransferTo(string toWarehouseId, string toBinId, decimal quantity, string reason, string performedBy)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        if (this.AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient available stock for transfer");

        this.ApplyChange(new StockTransferredEvent(this.Id, this.WarehouseId, this.BinId, toWarehouseId, toBinId, quantity, reason, performedBy));
    }

    public void IssueStock(decimal quantity, string sourceType, string sourceId, string performedBy, Guid? relatedReservationId = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        if (this.AvailableQuantity < quantity && relatedReservationId == null) 
            throw new InvalidOperationException("Insufficient available stock");

        // FIFO Logic
        decimal remainingToIssue = quantity;
        decimal totalCost = 0;
        List<StockBatch> batchesToConsume = this._batches.OrderBy(b => b.ReceivedDate).Where(b => b.RemainingQuantity > 0).ToList();

        foreach (StockBatch batch in batchesToConsume)
        {
            if (remainingToIssue <= 0) break;

            decimal qtyToTake = Math.Min(batch.RemainingQuantity, remainingToIssue);
            totalCost += qtyToTake * batch.UnitCost;
            
            // Note: We don't mutate state here in the command method, we assume the Event Handler will do it 
            // BUT for Aggregate consistency, we usually calculate what TO emit. 
            // Since we need to emit the CostAmount, we have to calculate it.
            // The actual mutation of _batches happens in Apply().
            
            remainingToIssue -= qtyToTake;
        }
        
        // If we found enough stock in batches, good. If not (e.g. data inconsistency or legacy stock without batches), 
        // we might fallback to current UnitCost or throw. For now, assuming batches cover OnHand.
        if (remainingToIssue > 0 && this.Batches.Any())
        {
             // Fallback: This shouldn't happen if OnHand quantity logic matches Batches sum.
             // But if it does, use the last known cost or 0? 
             // Let's assume strict consistency for now.
        }

        this.ApplyChange(new StockIssuedEvent(this.Id, quantity, totalCost, sourceType, sourceId, performedBy));
        
        if (relatedReservationId.HasValue)
        {
            this.ApplyChange(new ReservationReleasedEvent(this.Id, relatedReservationId.Value, quantity, "Stock Issued"));
        }
    }

    public void ReserveStock(Guid reservationId, decimal quantity, string sourceType, string sourceId, DateTime? expiryDate)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        if (this.AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient stock for reservation");

        this.ApplyChange(new StockReservedEvent(this.Id, reservationId, quantity, sourceType, sourceId, expiryDate));
    }

    public void ReleaseReservation(Guid reservationId, decimal quantity, string reason)
    {
        this.ApplyChange(new ReservationReleasedEvent(this.Id, reservationId, quantity, reason));
    }

    public void AdjustStock(decimal newQuantity, string reason, string performedBy)
    {
        decimal difference = newQuantity - this.OnHandQuantity;
        this.ApplyChange(new StockAdjustedEvent(this.Id, newQuantity, difference, reason, performedBy));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case InventoryItemCreatedEvent e:
                this.Id = e.InventoryItemId;
                this.WarehouseId = e.WarehouseId;
                this.BinId = e.BinId;
                this.MaterialId = e.MaterialId;
                break;

            case StockReceivedEvent e:
                this.OnHandQuantity += e.Quantity;
                this._batches.Add(new StockBatch(e.PerformedBy, e.Quantity, e.UnitCost, e.OccurredOn));
                break;

            case StockTransferredEvent e:
                // For simplified ES, a transfer within the same aggregate just updates the location
                // If it were a different aggregate, it would be Issue + Receive
                this.WarehouseId = e.ToWarehouseId;
                this.BinId = e.ToBinId;
                break;

            case StockIssuedEvent e:
                this.OnHandQuantity -= e.Quantity;
                
                // Update Batches (Mutation)
                decimal remainingToRemove = e.Quantity;
                // We must iterate ensuring we pick the same batches as the command method calculated (FIFO)
                // Since this is deterministic (Sorted by Date), it should be fine.
                foreach (StockBatch batch in this._batches.OrderBy(b => b.ReceivedDate).Where(b => b.RemainingQuantity > 0))
                {
                    if (remainingToRemove <= 0) break;
                    decimal qtyRemoved = Math.Min(batch.RemainingQuantity, remainingToRemove);
                    batch.RemainingQuantity -= qtyRemoved;
                    remainingToRemove -= qtyRemoved;
                }

                // Cleanup empty batches? Optional, but keeps list small.
                this._batches.RemoveAll(b => b.RemainingQuantity <= 0);
                break;

            case StockReservedEvent e:
                this.ReservedQuantity += e.Quantity;
                this._activeReservations.Add(e.ReservationId);
                break;

            case ReservationReleasedEvent e:
                this.ReservedQuantity -= e.Quantity;
                this._activeReservations.Remove(e.ReservationId);
                break;

            case StockAdjustedEvent e:
                this.OnHandQuantity = e.NewQuantity;
                break;
        }
    }
}

public record StockBatch(string BatchId, decimal OriginalQuantity, decimal UnitCost, DateTime ReceivedDate)
{
    public decimal RemainingQuantity { get; internal set; } = OriginalQuantity;
}
