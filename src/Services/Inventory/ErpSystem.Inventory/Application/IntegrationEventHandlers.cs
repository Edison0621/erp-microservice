using MediatR;
using ErpSystem.Procurement.Domain;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.Production.Domain;
using ErpSystem.Sales.Domain;

namespace ErpSystem.Inventory.Application;

// Since we are in the Inventory service, we define the handler for the INTEGRATION event.
// The integration event record should ideally be shared or duplicated if services are decoupled.
// For now, we reference it from Procurement.Domain (which is in the same solution).
// If we wanted true decoupling, we'd define a local copy.

public class ProcurementIntegrationEventHandler(IMediator mediator) : INotificationHandler<ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent>
{
    public async Task Handle(ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent n, CancellationToken ct)
    {
        foreach (ProcurementIntegrationEvents.GoodsReceivedItem item in n.Items)
        {
            await mediator.Send(new ReceiveStockCommand(
                item.WarehouseId,
                "DEFAULT_BIN", // Default bin
                item.MaterialId,
                item.Quantity,
                0, // UnitCost unknown from event
                "PO_RECEIPT",
                n.PurchaseOrderId.ToString(),
                "SYSTEM"
            ), ct);
        }
    }
}

public class SalesIntegrationEventHandler(IMediator mediator) :
    INotificationHandler<SalesIntegrationEvents.OrderConfirmedIntegrationEvent>,
    INotificationHandler<SalesIntegrationEvents.ShipmentCreatedIntegrationEvent>
{
    public async Task Handle(SalesIntegrationEvents.OrderConfirmedIntegrationEvent n, CancellationToken ct)
    {
        foreach (SalesIntegrationEvents.OrderConfirmedItem item in n.Items)
        {
            // First find or create the inventory item to get its ID
            InventoryItemReadModel? info = await mediator.Send(new GetInventoryItemQuery(item.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await mediator.Send(new ReserveStockCommand(
                    info.Id,
                    item.Quantity,
                    "SALES_ORDER",
                    n.SoNumber,
                    null
                ), ct);
            }
        }
    }

    public async Task Handle(SalesIntegrationEvents.ShipmentCreatedIntegrationEvent n, CancellationToken ct)
    {
        foreach (SalesIntegrationEvents.ShipmentItem item in n.Items)
        {
            InventoryItemReadModel? info = await mediator.Send(new GetInventoryItemQuery(n.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await mediator.Send(new IssueStockCommand(
                    info.Id,
                    item.Quantity,
                    "SHIPMENT",
                    n.ShipmentId.ToString(),
                    "SYSTEM"
                ), ct);
            }
        }
    }
}

public class ProductionIntegrationEventHandler(IMediator mediator) :
    INotificationHandler<ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent>,
    INotificationHandler<ProductionIntegrationEvents.ProductionCompletedIntegrationEvent>
{
    public async Task Handle(ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent n, CancellationToken ct)
    {
        foreach (ProductionIntegrationEvents.MaterialIssueItem item in n.Items)
        {
            InventoryItemReadModel? info = await mediator.Send(new GetInventoryItemQuery(n.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await mediator.Send(new IssueStockCommand(
                    info.Id,
                    item.Quantity,
                    "PROD_ISSUE",
                    n.OrderId.ToString(),
                    "SYSTEM"
                ), ct);
            }
        }
    }

    public async Task Handle(ProductionIntegrationEvents.ProductionCompletedIntegrationEvent n, CancellationToken ct)
    {
        await mediator.Send(new ReceiveStockCommand(
            n.WarehouseId,
            "DEFAULT_BIN",
            n.MaterialId,
            n.Quantity,
            0,
            "PROD_FINISH",
            n.OrderId.ToString(),
            "SYSTEM"
        ), ct);
    }
}
