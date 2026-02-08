using Dapr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application.IntegrationEvents;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/integration")]
public class IntegrationEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntegrationEventsController(IMediator mediator) => _mediator = mediator;

    // [Topic("pubsub", "EmployeeHiredIntegrationEvent")]
    [HttpPost("employee-hired")]
    public async Task<IActionResult> HandleHired(HRIntegrationEvents.EmployeeHiredIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }

    // [Topic("pubsub", "EmployeeTerminatedIntegrationEvent")]
    [HttpPost("employee-terminated")]
    public async Task<IActionResult> HandleTerminated(HRIntegrationEvents.EmployeeTerminatedIntegrationEvent @event)
    {
        await _mediator.Publish(@event);
        return Ok();
    }
}
