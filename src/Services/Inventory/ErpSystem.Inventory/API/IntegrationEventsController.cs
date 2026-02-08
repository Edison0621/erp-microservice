using Dapr;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Inventory.API;

[ApiController]
[Route("api/v1/inventory/integration")]
public class IntegrationEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntegrationEventsController(IMediator mediator) => _mediator = mediator;

    // [Topic("pubsub", "GoodsReceivedIntegrationEvent")]
    [HttpPost("goods-received")]
    public async Task<IActionResult> HandleGoodsReceived(ErpSystem.Procurement.Domain.ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }

    // [Topic("pubsub", "OrderConfirmedIntegrationEvent")]
    [HttpPost("order-confirmed")]
    public async Task<IActionResult> HandleOrderConfirmed(ErpSystem.Sales.Domain.SalesIntegrationEvents.OrderConfirmedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }

    // [Topic("pubsub", "ShipmentCreatedIntegrationEvent")]
    [HttpPost("shipment-created")]
    public async Task<IActionResult> HandleShipmentCreated(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }

    // [Topic("pubsub", "ProductionMaterialIssuedIntegrationEvent")]
    [HttpPost("production-material-issued")]
    public async Task<IActionResult> HandleProductionMaterialIssued(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionMaterialIssuedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }

    // [Topic("pubsub", "ProductionCompletedIntegrationEvent")]
    [HttpPost("production-completed")]
    public async Task<IActionResult> HandleProductionCompleted(ErpSystem.Production.Domain.ProductionIntegrationEvents.ProductionCompletedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }
}
