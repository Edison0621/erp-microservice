using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Inventory.API;

[ApiController]
[Route("api/v1/inventory/integration")]
public class IntegrationEventsController(IMediator mediator) : ControllerBase
{
    // [Topic("pubsub", "GoodsReceivedIntegrationEvent")]
    [HttpPost("goods-received")]
    public async Task<IActionResult> HandleGoodsReceived(ErpSystem.Procurement.Domain.ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }

    // [Topic("pubsub", "OrderConfirmedIntegrationEvent")]
    [HttpPost("order-confirmed")]
    public async Task<IActionResult> HandleOrderConfirmed(ErpSystem.Sales.Domain.SalesIntegrationEvents.OrderConfirmedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }

    // [Topic("pubsub", "ShipmentCreatedIntegrationEvent")]
    [HttpPost("shipment-created")]
    public async Task<IActionResult> HandleShipmentCreated(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }

    // [Topic("pubsub", "ProductionMaterialIssuedIntegrationEvent")]
    [HttpPost("production-material-issued")]
    public async Task<IActionResult> HandleProductionMaterialIssued(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }

    // [Topic("pubsub", "ProductionCompletedIntegrationEvent")]
    [HttpPost("production-completed")]
    public async Task<IActionResult> HandleProductionCompleted(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionCompletedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }
}
