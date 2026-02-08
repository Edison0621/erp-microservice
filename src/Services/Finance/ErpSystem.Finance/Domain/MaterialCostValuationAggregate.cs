using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Domain;

/// <summary>
/// Material Cost Valuation Aggregate - Manages moving average cost for materials
/// </summary>
public class MaterialCostValuation : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public string WarehouseId { get; private set; } = string.Empty;
    public decimal CurrentAverageCost { get; private set; }
    public decimal TotalQuantityOnHand { get; private set; }
    public decimal TotalValue { get; private set; }
    public DateTime LastUpdated { get; private set; }

    public static MaterialCostValuation Create(
        Guid id,
        string tenantId,
        string materialId,
        string warehouseId,
        decimal initialCost)
    {
        var valuation = new MaterialCostValuation();
        valuation.ApplyChange(new MaterialCostValuationCreatedEvent(
            id,
            tenantId,
            materialId,
            warehouseId,
            initialCost,
            DateTime.UtcNow));
        return valuation;
    }

    /// <summary>
    /// Process goods receipt - updates moving average cost
    public void ProcessReceipt(
        string sourceId,
        string sourceType,
        decimal quantity,
        decimal unitCost,
        DateTime occurredAt)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Receipt quantity must be positive");

        var receiptValue = quantity * unitCost;
        var newTotalValue = TotalValue + receiptValue;
        var newTotalQuantity = TotalQuantityOnHand + quantity;
        var newAverageCost = newTotalQuantity > 0 ? newTotalValue / newTotalQuantity : 0;

        ApplyChange(new MaterialReceiptProcessedEvent(
            Id,
            TenantId,
            MaterialId,
            WarehouseId,
            sourceId,
            sourceType,
            quantity,
            unitCost,
            receiptValue,
            newAverageCost,
            newTotalQuantity,
            newTotalValue,
            occurredAt));
    }

    /// <summary>
    /// Process goods issue - uses current average cost
    /// </summary>
    public void ProcessIssue(
        string sourceId,
        string sourceType,
        decimal quantity,
        DateTime occurredAt)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Issue quantity must be positive");

        if (quantity > TotalQuantityOnHand)
            throw new InvalidOperationException($"Insufficient quantity. Available: {TotalQuantityOnHand}, Requested: {quantity}");

        var issueValue = quantity * CurrentAverageCost;
        var newTotalValue = TotalValue - issueValue;
        var newTotalQuantity = TotalQuantityOnHand - quantity;

        ApplyChange(new MaterialIssueProcessedEvent(
            Id,
            TenantId,
            MaterialId,
            WarehouseId,
            sourceId,
            sourceType,
            quantity,
            CurrentAverageCost,
            issueValue,
            newTotalQuantity,
            newTotalValue,
            occurredAt));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case MaterialCostValuationCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                MaterialId = e.MaterialId;
                WarehouseId = e.WarehouseId;
                CurrentAverageCost = e.InitialCost;
                TotalQuantityOnHand = 0;
                TotalValue = 0;
                LastUpdated = e.OccurredAt;
                break;

            case MaterialReceiptProcessedEvent e:
                TotalValue = e.NewTotalValue;
                TotalQuantityOnHand = e.NewTotalQuantity;
                CurrentAverageCost = e.NewAverageCost;
                LastUpdated = e.OccurredAt;
                break;

            case MaterialIssueProcessedEvent e:
                TotalValue = e.NewTotalValue;
                TotalQuantityOnHand = e.NewTotalQuantity;
                LastUpdated = e.OccurredAt;
                break;
        }
    }
}

// Domain Events
public record MaterialCostValuationCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string MaterialId,
    string WarehouseId,
    decimal InitialCost,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record MaterialReceiptProcessedEvent(
    Guid AggregateId,
    string TenantId,
    string MaterialId,
    string WarehouseId,
    string SourceId,
    string SourceType,
    decimal Quantity,
    decimal UnitCost,
    decimal ReceiptValue,
    decimal NewAverageCost,
    decimal NewTotalQuantity,
    decimal NewTotalValue,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record MaterialIssueProcessedEvent(
    Guid AggregateId,
    string TenantId,
    string MaterialId,
    string WarehouseId,
    string SourceId,
    string SourceType,
    decimal Quantity,
    decimal AverageCost,
    decimal IssueValue,
    decimal NewTotalQuantity,
    decimal NewTotalValue,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
