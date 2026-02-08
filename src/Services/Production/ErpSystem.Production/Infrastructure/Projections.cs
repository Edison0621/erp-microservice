using MediatR;
using ErpSystem.Production.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Production.Infrastructure;

public class ProductionProjections : 
    INotificationHandler<ProductionOrderCreatedEvent>,
    INotificationHandler<ProductionOrderReleasedEvent>,
    INotificationHandler<MaterialConsumedEvent>,
    INotificationHandler<ProductionReportedEvent>,
    INotificationHandler<ProductionOrderCompletedEvent>
{
    private readonly ProductionReadDbContext _readDb;

    public ProductionProjections(ProductionReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task Handle(ProductionOrderCreatedEvent n, CancellationToken ct)
    {
        var model = new ProductionOrderReadModel
        {
            Id = n.OrderId,
            OrderNumber = n.OrderNumber,
            MaterialId = n.MaterialId,
            MaterialCode = n.MaterialCode,
            MaterialName = n.MaterialName,
            PlannedQuantity = n.PlannedQuantity,
            Status = ProductionOrderStatus.Created.ToString(),
            CreatedDate = n.CreatedDate
        };
        _readDb.ProductionOrders.Add(model);
        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(ProductionOrderReleasedEvent n, CancellationToken ct)
    {
        var order = await _readDb.ProductionOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (order != null)
        {
            order.Status = ProductionOrderStatus.Released.ToString();
            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MaterialConsumedEvent n, CancellationToken ct)
    {
        var order = await _readDb.ProductionOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (order != null)
        {
            if (string.Equals(order.Status, ProductionOrderStatus.Released.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                order.Status = ProductionOrderStatus.InProgress.ToString();
                order.ActualStartDate ??= n.OccurredOn;
            }
            
            _readDb.MaterialConsumptions.Add(new MaterialConsumptionReadModel
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = n.OrderId,
                MaterialId = n.MaterialId,
                WarehouseId = n.WarehouseId,
                Quantity = n.Quantity,
                ConsumedAt = n.OccurredOn,
                ConsumedBy = n.ConsumedBy
            });
            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProductionReportedEvent n, CancellationToken ct)
    {
        var order = await _readDb.ProductionOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (order != null)
        {
            if (string.Equals(order.Status, ProductionOrderStatus.Released.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                order.Status = ProductionOrderStatus.InProgress.ToString();
                order.ActualStartDate ??= n.OccurredOn;
            }

            order.ReportedQuantity += n.GoodQuantity;
            order.ScrappedQuantity += n.ScrapQuantity;
            
            if (order.ReportedQuantity > 0 && order.ReportedQuantity < order.PlannedQuantity)
                order.Status = ProductionOrderStatus.PartiallyCompleted.ToString();

            _readDb.ProductionReports.Add(new ProductionReportReadModel
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = n.OrderId,
                ReportedAt = n.OccurredOn,
                GoodQuantity = n.GoodQuantity,
                ScrapQuantity = n.ScrapQuantity,
                WarehouseId = n.WarehouseId,
                ReportedBy = n.ReportedBy
            });
            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProductionOrderCompletedEvent n, CancellationToken ct)
    {
        var order = await _readDb.ProductionOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (order != null)
        {
            order.Status = ProductionOrderStatus.Completed.ToString();
            order.ActualEndDate = n.OccurredOn;
            await _readDb.SaveChangesAsync(ct);
        }
    }
}
