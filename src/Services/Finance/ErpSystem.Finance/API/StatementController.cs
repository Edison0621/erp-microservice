using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/v1/finance/statements")]
public class StatementController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        StatementDto? stmt = await mediator.Send(new GetStatementQuery(id));
        if (stmt == null) return this.NotFound();
        return this.Ok(stmt);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string supplierId, [FromQuery] string period)
    {
        StatementDto? stmt = await mediator.Send(new GetStatementByPeriodQuery(supplierId, period));
        if (stmt == null) return this.NotFound();
        return this.Ok(stmt);
    }

    [HttpPost("{id}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id)
    {
        await mediator.Send(new ReconcileStatementCommand(id));
        return this.NoContent();
    }
}
