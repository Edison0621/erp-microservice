using MediatR;
using ErpSystem.Production.Domain;

namespace ErpSystem.Production.Infrastructure;

public class ProductionProjections(ProductionReadDbContext readDb) :
    INotificationHandler<ProductionOrderCreatedEvent>,
    INotificationHandler<ProductionOrderReleasedEvent>,
    INotificationHandler<MaterialConsumedEvent>,
    INotificationHandler<ProductionReportedEvent>,
    INotificationHandler<ProductionOrderCompletedEvent>
{
    public async Task Handle(ProductionOrderCreatedEvent n, CancellationToken ct)
    {
        ProductionOrderReadModel model = new()
        {
            Id = n.OrderId,
            OrderNumber = n.OrderNumber,
            MaterialId = n.MaterialId,
            MaterialCode = n.MaterialCode,
            MaterialName = n.MaterialName,
            PlannedQuantity = n.PlannedQuantity,
            Status = nameof(ProductionOrderStatus.Created),
            CreatedDate = n.CreatedDate
        };
        readDb.ProductionOrders.Add(model);
        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(ProductionOrderReleasedEvent n, CancellationToken ct)
    {
        ProductionOrderReadModel? order = await readDb.ProductionOrders.FindAsync([n.OrderId], ct);
        if (order != null)
        {
            order.Status = nameof(ProductionOrderStatus.Released);
            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MaterialConsumedEvent n, CancellationToken ct)
    {
        ProductionOrderReadModel? order = await readDb.ProductionOrders.FindAsync([n.OrderId], ct);
        if (order != null)
        {
            if (string.Equals(order.Status, nameof(ProductionOrderStatus.Released), StringComparison.OrdinalIgnoreCase))
            {
                order.Status = nameof(ProductionOrderStatus.InProgress);
                order.ActualStartDate ??= n.OccurredOn;
            }

            readDb.MaterialConsumptions.Add(new MaterialConsumptionReadModel
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = n.OrderId,
                MaterialId = n.MaterialId,
                WarehouseId = n.WarehouseId,
                Quantity = n.Quantity,
                ConsumedAt = n.OccurredOn,
                ConsumedBy = n.ConsumedBy
            });
            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProductionReportedEvent n, CancellationToken ct)
    {
        ProductionOrderReadModel? order = await readDb.ProductionOrders.FindAsync([n.OrderId], ct);
        if (order != null)
        {
            if (string.Equals(order.Status, nameof(ProductionOrderStatus.Released), StringComparison.OrdinalIgnoreCase))
            {
                order.Status = nameof(ProductionOrderStatus.InProgress);
                order.ActualStartDate ??= n.OccurredOn;
            }

            order.ReportedQuantity += n.GoodQuantity;
            order.ScrappedQuantity += n.ScrapQuantity;
            
            if (order.ReportedQuantity > 0 && order.ReportedQuantity < order.PlannedQuantity)
                order.Status = nameof(ProductionOrderStatus.PartiallyCompleted);

            readDb.ProductionReports.Add(new ProductionReportReadModel
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = n.OrderId,
                ReportedAt = n.OccurredOn,
                GoodQuantity = n.GoodQuantity,
                ScrapQuantity = n.ScrapQuantity,
                WarehouseId = n.WarehouseId,
                ReportedBy = n.ReportedBy
            });
            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProductionOrderCompletedEvent n, CancellationToken ct)
    {
        ProductionOrderReadModel? order = await readDb.ProductionOrders.FindAsync([n.OrderId], ct);
        if (order != null)
        {
            order.Status = nameof(ProductionOrderStatus.Completed);
            order.ActualEndDate = n.OccurredOn;
            await readDb.SaveChangesAsync(ct);
        }
    }
}
