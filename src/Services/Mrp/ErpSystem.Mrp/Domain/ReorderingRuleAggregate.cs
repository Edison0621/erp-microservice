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

        var rule = new ReorderingRule();
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

        ApplyChange(new ReorderingRuleQuantitiesUpdatedEvent(
            Id,
            minQuantity,
            maxQuantity,
            reorderQuantity ?? (maxQuantity - minQuantity),
            DateTime.UtcNow));
    }

    public void UpdateLeadTime(int leadTimeDays)
    {
        if (leadTimeDays < 0)
            throw new InvalidOperationException("Lead time cannot be negative");

        ApplyChange(new ReorderingRuleLeadTimeUpdatedEvent(Id, leadTimeDays, DateTime.UtcNow));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        ApplyChange(new ReorderingRuleActivatedEvent(Id, DateTime.UtcNow));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        ApplyChange(new ReorderingRuleDeactivatedEvent(Id, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ReorderingRuleCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                MaterialId = e.MaterialId;
                WarehouseId = e.WarehouseId;
                MinQuantity = e.MinQuantity;
                MaxQuantity = e.MaxQuantity;
                ReorderQuantity = e.ReorderQuantity;
                LeadTimeDays = e.LeadTimeDays;
                Strategy = e.Strategy;
                IsActive = true;
                break;
            case ReorderingRuleQuantitiesUpdatedEvent e:
                MinQuantity = e.MinQuantity;
                MaxQuantity = e.MaxQuantity;
                ReorderQuantity = e.ReorderQuantity;
                break;
            case ReorderingRuleLeadTimeUpdatedEvent e:
                LeadTimeDays = e.LeadTimeDays;
                break;
            case ReorderingRuleActivatedEvent:
                IsActive = true;
                break;
            case ReorderingRuleDeactivatedEvent:
                IsActive = false;
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
    public DateTime OccurredOn => OccurredAt;
}

public record ReorderingRuleQuantitiesUpdatedEvent(
    Guid AggregateId,
    decimal MinQuantity,
    decimal MaxQuantity,
    decimal ReorderQuantity,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record ReorderingRuleLeadTimeUpdatedEvent(
    Guid AggregateId,
    int LeadTimeDays,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record ReorderingRuleActivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

public record ReorderingRuleDeactivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}

