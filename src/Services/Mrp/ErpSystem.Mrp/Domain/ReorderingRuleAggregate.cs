using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Mrp.Domain;

/// <summary>
/// Reordering Rule Aggregate - Defines automatic replenishment rules for materials
/// Based on Odoo's reordering rules concept
/// </summary>
public class ReorderingRule : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public string WarehouseId { get; private set; } = string.Empty;
    public decimal MinQuantity { get; private set; }
    public decimal MaxQuantity { get; private set; }
    public decimal ReorderQuantity { get; private set; }
    public int LeadTimeDays { get; private set; }
    public ReorderingStrategy Strategy { get; private set; }
    public bool IsActive { get; private set; }

    public static ReorderingRule Create(
        Guid id,
        string tenantId,
        string materialId,
        string warehouseId,
        decimal minQuantity,
        decimal maxQuantity,
        int leadTimeDays,
        ReorderingStrategy strategy)
    {
        if (minQuantity < 0)
            throw new InvalidOperationException("Min quantity cannot be negative");
        
        if (maxQuantity <= minQuantity)
            throw new InvalidOperationException("Max quantity must be greater than min quantity");

        ReorderingRule rule = new();
        rule.ApplyChange(new ReorderingRuleCreatedEvent(
            id,
            tenantId,
            materialId,
            warehouseId,
            minQuantity,
            maxQuantity,
            maxQuantity - minQuantity, // Default reorder quantity
            leadTimeDays,
            strategy,
            DateTime.UtcNow));
        return rule;
    }

    public void UpdateQuantities(decimal minQuantity, decimal maxQuantity, decimal? reorderQuantity = null)
    {
        if (minQuantity < 0)
            throw new InvalidOperationException("Min quantity cannot be negative");
        
        if (maxQuantity <= minQuantity)
            throw new InvalidOperationException("Max quantity must be greater than min quantity");

        this.ApplyChange(new ReorderingRuleQuantitiesUpdatedEvent(this.Id,
            minQuantity,
            maxQuantity,
            reorderQuantity ?? (maxQuantity - minQuantity),
            DateTime.UtcNow));
    }

    public void UpdateLeadTime(int leadTimeDays)
    {
        if (leadTimeDays < 0)
            throw new InvalidOperationException("Lead time cannot be negative");

        this.ApplyChange(new ReorderingRuleLeadTimeUpdatedEvent(this.Id, leadTimeDays, DateTime.UtcNow));
    }

    public void Activate()
    {
        if (this.IsActive)
            return;

        this.ApplyChange(new ReorderingRuleActivatedEvent(this.Id, DateTime.UtcNow));
    }

    public void Deactivate()
    {
        if (!this.IsActive)
            return;

        this.ApplyChange(new ReorderingRuleDeactivatedEvent(this.Id, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ReorderingRuleCreatedEvent e:
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.MaterialId = e.MaterialId;
                this.WarehouseId = e.WarehouseId;
                this.MinQuantity = e.MinQuantity;
                this.MaxQuantity = e.MaxQuantity;
                this.ReorderQuantity = e.ReorderQuantity;
                this.LeadTimeDays = e.LeadTimeDays;
                this.Strategy = e.Strategy;
                this.IsActive = true;
                break;
            case ReorderingRuleQuantitiesUpdatedEvent e:
                this.MinQuantity = e.MinQuantity;
                this.MaxQuantity = e.MaxQuantity;
                this.ReorderQuantity = e.ReorderQuantity;
                break;
            case ReorderingRuleLeadTimeUpdatedEvent e:
                this.LeadTimeDays = e.LeadTimeDays;
                break;
            case ReorderingRuleActivatedEvent:
                this.IsActive = true;
                break;
            case ReorderingRuleDeactivatedEvent:
                this.IsActive = false;
                break;
        }
    }
}

public enum ReorderingStrategy
{
    /// <summary>Make to Stock - Trigger procurement when stock is low</summary>
    MakeToStock = 1,
    
    /// <summary>Make to Order - Only procure when there's a confirmed order</summary>
    MakeToOrder = 2,
    
    /// <summary>Hybrid - Maintain safety stock but also support MTO</summary>
    Hybrid = 3
}

// Domain Events
public record ReorderingRuleCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string MaterialId,
    string WarehouseId,
    decimal MinQuantity,
    decimal MaxQuantity,
    decimal ReorderQuantity,
    int LeadTimeDays,
    ReorderingStrategy Strategy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record ReorderingRuleQuantitiesUpdatedEvent(
    Guid AggregateId,
    decimal MinQuantity,
    decimal MaxQuantity,
    decimal ReorderQuantity,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record ReorderingRuleLeadTimeUpdatedEvent(
    Guid AggregateId,
    int LeadTimeDays,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record ReorderingRuleActivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record ReorderingRuleDeactivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

