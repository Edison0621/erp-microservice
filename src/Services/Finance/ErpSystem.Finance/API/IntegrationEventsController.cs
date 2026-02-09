using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/v1/finance/integration")]
public class IntegrationEventsController(IMediator mediator) : ControllerBase
{
    // [Topic("pubsub", "ShipmentCreatedIntegrationEvent")]
    [HttpPost("shipment-created")]
    public async Task<IActionResult> HandleShipmentCreated(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }
}
