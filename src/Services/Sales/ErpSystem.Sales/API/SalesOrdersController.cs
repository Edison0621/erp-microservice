using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Sales.Application;

namespace ErpSystem.Sales.API;

[ApiController]
[Route("api/v1/sales/orders")]
public class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSoCommand command) => this.Ok(await mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await mediator.Send(new GetSoByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? customerId, [FromQuery] string? status, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new SearchSOsQuery(customerId, status, page)));

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromQuery] string warehouseId)
        =>
            this.Ok(await mediator.Send(new ConfirmSoCommand(id, warehouseId)));

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
        =>
            this.Ok(await mediator.Send(new CancelSoCommand(id, reason)));

    [HttpGet("{id}/billable-lines")]
    public async Task<IActionResult> GetBillableLines(Guid id)
        =>
            this.Ok(await mediator.Send(new GetBillableLinesQuery(id)));
}

[ApiController]
[Route("api/v1/sales/shipments")]
public class ShipmentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateShipmentCommand command) => this.Ok(await mediator.Send(command));
}
