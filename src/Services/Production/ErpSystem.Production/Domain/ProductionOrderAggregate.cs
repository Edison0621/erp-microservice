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
        ProductionOrder order = new ProductionOrder();
        order.ApplyChange(new ProductionOrderCreatedEvent(id, orderNumber, materialId, materialCode, materialName, plannedQuantity, DateTime.UtcNow));
        return order;
    }

    public void Release()
    {
        if (this.Status != ProductionOrderStatus.Created)
            throw new InvalidOperationException("Only Created orders can be released");
        this.ApplyChange(new ProductionOrderReleasedEvent(this.Id));
    }

    public void ConsumeMaterial(string materialId, string warehouseId, decimal quantity, string consumedBy)
    {
        if (this.Status != ProductionOrderStatus.Released && this.Status != ProductionOrderStatus.InProgress)
            throw new InvalidOperationException("Order must be released or in progress to consume material");

        this.ApplyChange(new MaterialConsumedEvent(this.Id, materialId, warehouseId, quantity, consumedBy));
    }

    public void ReportProduction(decimal goodQuantity, decimal scrapQuantity, string warehouseId, string reportedBy)
    {
        if (this.Status != ProductionOrderStatus.Released && this.Status != ProductionOrderStatus.InProgress && this.Status != ProductionOrderStatus.PartiallyCompleted)
            throw new InvalidOperationException("Order state invalid for reporting");

        this.ApplyChange(new ProductionReportedEvent(this.Id, goodQuantity, scrapQuantity, warehouseId, reportedBy));

        if (this.ReportedQuantity >= this.PlannedQuantity)
        {
            this.ApplyChange(new ProductionOrderCompletedEvent(this.Id));
        }
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProductionOrderCreatedEvent e:
                this.Id = e.OrderId;
                this.OrderNumber = e.OrderNumber;
                this.MaterialId = e.MaterialId;
                this.PlannedQuantity = e.PlannedQuantity;
                this.Status = ProductionOrderStatus.Created;
                break;
            case ProductionOrderReleasedEvent:
                this.Status = ProductionOrderStatus.Released;
                break;
            case MaterialConsumedEvent:
                if (this.Status == ProductionOrderStatus.Released) this.Status = ProductionOrderStatus.InProgress;
                break;
            case ProductionReportedEvent e:
                if (this.Status == ProductionOrderStatus.Released) this.Status = ProductionOrderStatus.InProgress;
                this.ReportedQuantity += e.GoodQuantity;
                this.ScrappedQuantity += e.ScrapQuantity;
                if (this.ReportedQuantity > 0 && this.ReportedQuantity < this.PlannedQuantity) this.Status = ProductionOrderStatus.PartiallyCompleted;
                break;
            case ProductionOrderCompletedEvent:
                this.Status = ProductionOrderStatus.Completed;
                break;
        }
    }
}
