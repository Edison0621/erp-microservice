using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/v1/finance/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly FinanceReadDbContext _readDb;

    public InvoicesController(IMediator mediator, FinanceReadDbContext readDb)
    {
        _mediator = mediator;
        _readDb = readDb;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _readDb.Invoices.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _readDb.Invoices.FindAsync(id));

    [HttpPost("{id}/issue")]
    public async Task<IActionResult> Issue(Guid id)
    {
        await _mediator.Send(new IssueInvoiceCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _mediator.Send(new CancelInvoiceCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/write-off")]
    public async Task<IActionResult> WriteOff(Guid id, [FromBody] string reason)
    {
        await _mediator.Send(new WriteOffInvoiceCommand(id, reason));
        return NoContent();
    }

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, RecordPaymentCommand command)
    {
        if (id != command.InvoiceId) return BadRequest();
        var paymentId = await _mediator.Send(command);
        return Ok(paymentId);
    }

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPayments(Guid id) 
        => Ok(await _readDb.Payments.Where(p => p.InvoiceId == id).ToListAsync());

    [HttpGet("aging-analysis")]
    public async Task<IActionResult> GetAgingAnalysis([FromQuery] int type, [FromQuery] DateTime? asOf, [FromQuery] string? partyId)
    {
        var result = await _mediator.Send(new GetAgingAnalysisQuery(type, asOf ?? DateTime.UtcNow, partyId));
        return Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueInvoices([FromQuery] int type, [FromQuery] DateTime? asOf, [FromQuery] string? partyId)
    {
        var result = await _mediator.Send(new GetOverdueInvoicesQuery(type, asOf ?? DateTime.UtcNow, partyId));
        return Ok(result);
    }
}
