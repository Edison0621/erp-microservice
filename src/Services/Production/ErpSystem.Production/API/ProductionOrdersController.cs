using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Production.Application;

namespace ErpSystem.Production.API;

[ApiController]
[Route("api/v1/production/orders")]
public class ProductionOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductionOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductionOrderCommand command) => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetProductionOrderByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? materialId, [FromQuery] string? status, [FromQuery] int page = 1)
        => Ok(await _mediator.Send(new SearchProductionOrdersQuery(materialId, status, page)));

    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(Guid id)
        => Ok(await _mediator.Send(new ReleaseProductionOrderCommand(id)));

    [HttpPost("{id}/consume")]
    public async Task<IActionResult> Consume(Guid id, ConsumeMaterialCommand command)
    {
        if (id != command.OrderId) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("{id}/report")]
    public async Task<IActionResult> Report(Guid id, ReportProductionCommand command)
    {
        if (id != command.OrderId) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("wip")]
    public async Task<IActionResult> GetWip([FromQuery] string? materialId)
        => Ok(await _mediator.Send(new GetProductionWipQuery(materialId)));
}
