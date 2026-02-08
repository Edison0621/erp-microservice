using Dapr;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/v1/finance/integration")]
public class IntegrationEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntegrationEventsController(IMediator mediator) => _mediator = mediator;

    // [Topic("pubsub", "ShipmentCreatedIntegrationEvent")]
    [HttpPost("shipment-created")]
    public async Task<IActionResult> HandleShipmentCreated(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }
}
