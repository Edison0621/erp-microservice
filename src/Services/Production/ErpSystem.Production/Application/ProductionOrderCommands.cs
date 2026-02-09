using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Production.Domain;

namespace ErpSystem.Production.Application;

public record CreateProductionOrderCommand(
    string MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal PlannedQuantity
) : IRequest<Guid>;

public record ReleaseProductionOrderCommand(Guid OrderId) : IRequest<bool>;

public record ConsumeMaterialCommand(
    Guid OrderId,
    string MaterialId,
    string WarehouseId,
    decimal Quantity,
    string ConsumedBy
) : IRequest<bool>;

public record ReportProductionCommand(
    Guid OrderId,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    string WarehouseId,
    string ReportedBy
) : IRequest<bool>;

public class ProductionOrderCommandHandler(EventStoreRepository<ProductionOrder> repo, IEventBus eventBus) :
    IRequestHandler<CreateProductionOrderCommand, Guid>,
    IRequestHandler<ReleaseProductionOrderCommand, bool>,
    IRequestHandler<ConsumeMaterialCommand, bool>,
    IRequestHandler<ReportProductionCommand, bool>
{
    public async Task<Guid> Handle(CreateProductionOrderCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string orderNumber = $"PRD-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        ProductionOrder order = ProductionOrder.Create(id, orderNumber, request.MaterialId, request.MaterialCode, request.MaterialName, request.PlannedQuantity);
        await repo.SaveAsync(order);
        return id;
    }

    public async Task<bool> Handle(ReleaseProductionOrderCommand request, CancellationToken ct)
    {
        ProductionOrder? order = await repo.LoadAsync(request.OrderId);
        if (order == null) throw new KeyNotFoundException("Order not found");
        order.Release();
        await repo.SaveAsync(order);
        return true;
    }

    public async Task<bool> Handle(ConsumeMaterialCommand request, CancellationToken ct)
    {
        ProductionOrder? order = await repo.LoadAsync(request.OrderId);
        if (order == null) throw new KeyNotFoundException("Order not found");
        
        order.ConsumeMaterial(request.MaterialId, request.WarehouseId, request.Quantity, request.ConsumedBy);
        await repo.SaveAsync(order);

        // Publish Integration Event for Inventory Issue
        await eventBus.PublishAsync(new ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent(
            order.Id,
            order.OrderNumber,
            request.WarehouseId,
            [new(request.MaterialId, request.Quantity)]
        ), ct);

        return true;
    }

    public async Task<bool> Handle(ReportProductionCommand request, CancellationToken ct)
    {
        ProductionOrder? order = await repo.LoadAsync(request.OrderId);
        if (order == null) throw new KeyNotFoundException("Order not found");

        order.ReportProduction(request.GoodQuantity, request.ScrapQuantity, request.WarehouseId, request.ReportedBy);
        await repo.SaveAsync(order);

        // Publish Integration Event for Inventory Receipt (for good quantity)
        if (request.GoodQuantity > 0)
        {
            await eventBus.PublishAsync(new ProductionIntegrationEvents.ProductionCompletedIntegrationEvent(
                order.Id,
                order.OrderNumber,
                order.MaterialId,
                request.WarehouseId,
                request.GoodQuantity
            ), ct);
        }

        return true;
    }
}
