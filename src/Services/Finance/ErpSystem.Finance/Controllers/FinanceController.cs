using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;

namespace ErpSystem.Finance.Controllers;

[ApiController]
[Route("api/v1/finance")]
public class FinanceController(IMediator mediator) : ControllerBase
{
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceCommand command) 
        =>
            this.Ok(await mediator.Send(command));

    [HttpPost("payments")]
    public async Task<IActionResult> RegisterPayment(RegisterPaymentCommand command) 
        =>
            this.Ok(await mediator.Send(command));
        
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        =>
            this.Ok(await mediator.Send(new GetInvoicesQuery(page, pageSize)));

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
        =>
            this.Ok(await mediator.Send(new GetInvoiceQuery(id)));

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        =>
            this.Ok(await mediator.Send(new GetPaymentsQuery(page, pageSize)));

    [HttpGet("reports/aging")]
    public async Task<IActionResult> GetAgingReport()
        =>
            this.Ok(await mediator.Send(new GetAgingReportQuery()));
}
