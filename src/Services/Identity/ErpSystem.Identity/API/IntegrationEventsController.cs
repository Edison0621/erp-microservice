using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application.IntegrationEvents;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/integration")]
public class IntegrationEventsController(IMediator mediator) : ControllerBase
{
    // [Topic("pubsub", "EmployeeHiredIntegrationEvent")]
    [HttpPost("employee-hired")]
    public async Task<IActionResult> HandleHired(HrIntegrationEvents.EmployeeHiredIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }

    // [Topic("pubsub", "EmployeeTerminatedIntegrationEvent")]
    [HttpPost("employee-terminated")]
    public async Task<IActionResult> HandleTerminated(HrIntegrationEvents.EmployeeTerminatedIntegrationEvent @event)
    {
        await mediator.Publish(@event);
        return this.Ok();
    }
}
