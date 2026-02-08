using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;

namespace ErpSystem.Finance.Controllers;

[ApiController]
[Route("api/v1/finance")]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator) => _mediator = mediator;

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceCommand command) 
        => Ok(await _mediator.Send(command));

    [HttpPost("payments")]
    public async Task<IActionResult> RegisterPayment(RegisterPaymentCommand command) 
        => Ok(await _mediator.Send(command));
        
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetInvoicesQuery(page, pageSize)));

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
        => Ok(await _mediator.Send(new GetInvoiceQuery(id)));

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetPaymentsQuery(page, pageSize)));

    [HttpGet("reports/aging")]
    public async Task<IActionResult> GetAgingReport()
        => Ok(await _mediator.Send(new GetAgingReportQuery()));
}
