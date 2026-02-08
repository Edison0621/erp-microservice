using MediatR;
using ErpSystem.Procurement.Domain;
using ErpSystem.Inventory.Application;

namespace ErpSystem.Inventory.Application;

// Since we are in the Inventory service, we define the handler for the INTEGRATION event.
// The integration event record should ideally be shared or duplicated if services are decoupled.
// For now, we reference it from Procurement.Domain (which is in the same solution).
// If we wanted true decoupling, we'd define a local copy.

public class ProcurementIntegrationEventHandler : INotificationHandler<ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public ProcurementIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent n, CancellationToken ct)
    {
        foreach (var item in n.Items)
        {
            await _mediator.Send(new ReceiveStockCommand(
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

public class SalesIntegrationEventHandler : 
    INotificationHandler<ErpSystem.Sales.Domain.SalesIntegrationEvents.OrderConfirmedIntegrationEvent>,
    INotificationHandler<ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public SalesIntegrationEventHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(ErpSystem.Sales.Domain.SalesIntegrationEvents.OrderConfirmedIntegrationEvent n, CancellationToken ct)
    {
        foreach (var item in n.Items)
        {
            // First find or create the inventory item to get its ID
            var info = await _mediator.Send(new GetInventoryItemQuery(item.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await _mediator.Send(new ReserveStockCommand(
                    info.Id,
                    item.Quantity,
                    "SALES_ORDER",
                    n.SONumber,
                    null
                ), ct);
            }
        }
    }

    public async Task Handle(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent n, CancellationToken ct)
    {
        foreach (var item in n.Items)
        {
            var info = await _mediator.Send(new GetInventoryItemQuery(n.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await _mediator.Send(new IssueStockCommand(
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

public class ProductionIntegrationEventHandler : 
    INotificationHandler<ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent>,
    INotificationHandler<ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionCompletedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public ProductionIntegrationEventHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent n, CancellationToken ct)
    {
        foreach (var item in n.Items)
        {
            var info = await _mediator.Send(new GetInventoryItemQuery(n.WarehouseId, "DEFAULT_BIN", item.MaterialId), ct);
            if (info != null)
            {
                await _mediator.Send(new IssueStockCommand(
                    info.Id,
                    item.Quantity,
                    "PROD_ISSUE",
                    n.OrderId.ToString(),
                    "SYSTEM"
                ), ct);
            }
        }
    }

    public async Task Handle(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionCompletedIntegrationEvent n, CancellationToken ct)
    {
        await _mediator.Send(new ReceiveStockCommand(
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
