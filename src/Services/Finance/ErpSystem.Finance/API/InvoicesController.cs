using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/v1/finance/invoices")]
public class InvoicesController(IMediator mediator, FinanceReadDbContext readDb) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => this.Ok(await readDb.Invoices.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await readDb.Invoices.FindAsync(id));

    [HttpPost("{id}/issue")]
    public async Task<IActionResult> Issue(Guid id)
    {
        await mediator.Send(new IssueInvoiceCommand(id));
        return this.NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await mediator.Send(new CancelInvoiceCommand(id));
        return this.NoContent();
    }

    [HttpPost("{id}/write-off")]
    public async Task<IActionResult> WriteOff(Guid id, [FromBody] string reason)
    {
        await mediator.Send(new WriteOffInvoiceCommand(id, reason));
        return this.NoContent();
    }

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, RecordPaymentCommand command)
    {
        if (id != command.InvoiceId) return this.BadRequest();
        Guid paymentId = await mediator.Send(command);
        return this.Ok(paymentId);
    }

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(Guid id) 
        =>
            this.Ok(await readDb.Payments.Where(p => p.InvoiceId == id).ToListAsync());

    [HttpGet("aging-analysis")]
    public async Task<IActionResult> GetAgingAnalysis([FromQuery] int type, [FromQuery] DateTime? asOf, [FromQuery] string? partyId)
    {
        List<AgingBucket> result = await mediator.Send(new GetAgingAnalysisQuery(type, asOf ?? DateTime.UtcNow, partyId));
        return this.Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueInvoices([FromQuery] int type, [FromQuery] DateTime? asOf, [FromQuery] string? partyId)
    {
        List<InvoiceReadModel> result = await mediator.Send(new GetOverdueInvoicesQuery(type, asOf ?? DateTime.UtcNow, partyId));
        return this.Ok(result);
    }
}
