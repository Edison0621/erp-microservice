using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Production.Application;

namespace ErpSystem.Production.API;

[ApiController]
[Route("api/v1/production/orders")]
public class ProductionOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductionOrderCommand command) => this.Ok(await mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await mediator.Send(new GetProductionOrderByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? materialId, [FromQuery] string? status, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new SearchProductionOrdersQuery(materialId, status, page)));

    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(Guid id)
        =>
            this.Ok(await mediator.Send(new ReleaseProductionOrderCommand(id)));

    [HttpPost("{id}/consume")]
    public async Task<IActionResult> Consume(Guid id, ConsumeMaterialCommand command)
    {
        if (id != command.OrderId) return this.BadRequest();
        return this.Ok(await mediator.Send(command));
    }

    [HttpPost("{id}/report")]
    public async Task<IActionResult> Report(Guid id, ReportProductionCommand command)
    {
        if (id != command.OrderId) return this.BadRequest();
        return this.Ok(await mediator.Send(command));
    }

    [HttpGet("wip")]
    public async Task<IActionResult> GetWip([FromQuery] string? materialId)
        =>
            this.Ok(await mediator.Send(new GetProductionWipQuery(materialId)));
}
