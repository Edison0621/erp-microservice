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
        MaterialCostValuation valuation = new MaterialCostValuation();
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
    /// </summary>
    public void ProcessReceipt(
        string sourceId,
        string sourceType,
        decimal quantity,
        decimal unitCost,
        DateTime occurredAt)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Receipt quantity must be positive");

        decimal receiptValue = quantity * unitCost;
        decimal newTotalValue = this.TotalValue + receiptValue;
        decimal newTotalQuantity = this.TotalQuantityOnHand + quantity;
        decimal newAverageCost = newTotalQuantity > 0 ? newTotalValue / newTotalQuantity : 0;

        this.ApplyChange(new MaterialReceiptProcessedEvent(this.Id, this.TenantId, this.MaterialId, this.WarehouseId,
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

        if (quantity > this.TotalQuantityOnHand)
            throw new InvalidOperationException($"Insufficient quantity. Available: {this.TotalQuantityOnHand}, Requested: {quantity}");

        decimal issueValue = quantity * this.CurrentAverageCost;
        decimal newTotalValue = this.TotalValue - issueValue;
        decimal newTotalQuantity = this.TotalQuantityOnHand - quantity;

        this.ApplyChange(new MaterialIssueProcessedEvent(this.Id, this.TenantId, this.MaterialId, this.WarehouseId,
            sourceId,
            sourceType,
            quantity, this.CurrentAverageCost,
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
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.MaterialId = e.MaterialId;
                this.WarehouseId = e.WarehouseId;
                this.CurrentAverageCost = e.InitialCost;
                this.TotalQuantityOnHand = 0;
                this.TotalValue = 0;
                this.LastUpdated = e.OccurredAt;
                break;

            case MaterialReceiptProcessedEvent e:
                this.TotalValue = e.NewTotalValue;
                this.TotalQuantityOnHand = e.NewTotalQuantity;
                this.CurrentAverageCost = e.NewAverageCost;
                this.LastUpdated = e.OccurredAt;
                break;

            case MaterialIssueProcessedEvent e:
                this.TotalValue = e.NewTotalValue;
                this.TotalQuantityOnHand = e.NewTotalQuantity;
                this.LastUpdated = e.OccurredAt;
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
    public DateTime OccurredOn => this.OccurredAt;
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
    public DateTime OccurredOn => this.OccurredAt;
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
    public DateTime OccurredOn => this.OccurredAt;
}
