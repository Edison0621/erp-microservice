using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Production.Domain;

public enum ProductionOrderStatus
{
    Created = 0,
    Released = 1,
    InProgress = 2,
    PartiallyCompleted = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6
}

// Events
public record ProductionOrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    string MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal PlannedQuantity,
    DateTime CreatedDate
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProductionOrderReleasedEvent(Guid OrderId) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaterialConsumedEvent(
    Guid OrderId,
    string MaterialId,
    string WarehouseId,
    decimal Quantity,
    string ConsumedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProductionReportedEvent(
    Guid OrderId,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    string WarehouseId,
    string ReportedBy
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProductionOrderCompletedEvent(Guid OrderId) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class ProductionOrder : AggregateRoot<Guid>
{
    public string OrderNumber { get; private set; } = string.Empty;
    public string MaterialId { get; private set; } = string.Empty;
    public decimal PlannedQuantity { get; private set; }
    public decimal ReportedQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }
    public ProductionOrderStatus Status { get; private set; }

    public static ProductionOrder Create(
        Guid id, 
        string orderNumber, 
        string materialId, 
        string materialCode, 
        string materialName, 
        decimal plannedQuantity)
    {
        var order = new ProductionOrder();
        order.ApplyChange(new ProductionOrderCreatedEvent(id, orderNumber, materialId, materialCode, materialName, plannedQuantity, DateTime.UtcNow));
        return order;
    }

    public void Release()
    {
        if (Status != ProductionOrderStatus.Created)
            throw new InvalidOperationException("Only Created orders can be released");
        ApplyChange(new ProductionOrderReleasedEvent(Id));
    }

    public void ConsumeMaterial(string materialId, string warehouseId, decimal quantity, string consumedBy)
    {
        if (Status != ProductionOrderStatus.Released && Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException("Order must be released or in progress to consume material");
            
        ApplyChange(new MaterialConsumedEvent(Id, materialId, warehouseId, quantity, consumedBy));
    }

    public void ReportProduction(decimal goodQuantity, decimal scrapQuantity, string warehouseId, string reportedBy)
    {
        if (Status != ProductionOrderStatus.Released && Status != ProductionOrderStatus.InProgress && Status != ProductionOrderStatus.PartiallyCompleted)
            throw new InvalidOperationException("Order state invalid for reporting");

        ApplyChange(new ProductionReportedEvent(Id, goodQuantity, scrapQuantity, warehouseId, reportedBy));

        if (ReportedQuantity >= PlannedQuantity)
        {
            ApplyChange(new ProductionOrderCompletedEvent(Id));
        }
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProductionOrderCreatedEvent e:
                Id = e.OrderId;
                OrderNumber = e.OrderNumber;
                MaterialId = e.MaterialId;
                PlannedQuantity = e.PlannedQuantity;
                Status = ProductionOrderStatus.Created;
                break;
            case ProductionOrderReleasedEvent:
                Status = ProductionOrderStatus.Released;
                break;
            case MaterialConsumedEvent:
                if (Status == ProductionOrderStatus.Released) Status = ProductionOrderStatus.InProgress;
                break;
            case ProductionReportedEvent e:
                if (Status == ProductionOrderStatus.Released) Status = ProductionOrderStatus.InProgress;
                ReportedQuantity += e.GoodQuantity;
                ScrappedQuantity += e.ScrapQuantity;
                if (ReportedQuantity > 0 && ReportedQuantity < PlannedQuantity)
                    Status = ProductionOrderStatus.PartiallyCompleted;
                break;
            case ProductionOrderCompletedEvent:
                Status = ProductionOrderStatus.Completed;
                break;
        }
    }
}
