using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Sales.Application;

namespace ErpSystem.Sales.API;

[ApiController]
[Route("api/v1/sales/orders")]
public class SalesOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateSOCommand command) => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetSOByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? customerId, [FromQuery] string? status, [FromQuery] int page = 1)
        => Ok(await _mediator.Send(new SearchSOsQuery(customerId, status, page)));

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromQuery] string warehouseId)
        => Ok(await _mediator.Send(new ConfirmSOCommand(id, warehouseId)));

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
        => Ok(await _mediator.Send(new CancelSOCommand(id, reason)));

    [HttpGet("{id}/billable-lines")]
    public async Task<IActionResult> GetBillableLines(Guid id)
        => Ok(await _mediator.Send(new GetBillableLinesQuery(id)));
}

[ApiController]
[Route("api/v1/sales/shipments")]
public class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShipmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateShipmentCommand command) => Ok(await _mediator.Send(command));
}
