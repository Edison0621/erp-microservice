using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;

namespace ErpSystem.Finance.Controllers;

[ApiController]
[Route("api/v1/finance")]
public class FinanceController(IMediator mediator) : ControllerBase
{
    [HttpPost("invoices/{id}/issue")]
    public async Task<IActionResult> IssueInvoice(Guid id)
    {
        await mediator.Send(new IssueInvoiceCommand(id));
        return this.Ok();
    }

    [HttpPost("invoices/{id}/cancel")]
    public async Task<IActionResult> CancelInvoice(Guid id)
    {
        await mediator.Send(new CancelInvoiceCommand(id));
        return this.Ok();
    }

    [HttpPost("invoices/{id}/write-off")]
    public async Task<IActionResult> WriteOffInvoice(Guid id, [FromBody] WriteOffInvoiceRequest request)
    {
        await mediator.Send(new WriteOffInvoiceCommand(id, request.Reason));
        return this.Ok();
    }

    [HttpPost("invoices/{id}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        await mediator.Send(new RecordPaymentCommand(id, request.Amount, request.PaymentDate, request.Method, request.ReferenceNo));
        return this.Ok();
    }

    [HttpGet("invoices/aging-analysis")]
    public async Task<IActionResult> GetAgingAnalysis([FromQuery] int type = 1, [FromQuery] string? partyId = null)
        => this.Ok(await mediator.Send(new GetAgingAnalysisQuery(type, DateTime.UtcNow, partyId)));

    [HttpGet("invoices/overdue")]
    public async Task<IActionResult> GetOverdueInvoices([FromQuery] int type = 1, [FromQuery] string? partyId = null)
        => this.Ok(await mediator.Send(new GetOverdueInvoicesQuery(type, DateTime.UtcNow, partyId)));

    [HttpGet("stats/dashboard")]
    public async Task<IActionResult> GetDashboardStats()
        => this.Ok(await mediator.Send(new GetFinancialDashboardStatsQuery()));

    // Keep existing endpoints
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceCommand command) 
        => this.Ok(await mediator.Send(command));

    [HttpPost("payments")]
    public async Task<IActionResult> RegisterPayment(RegisterPaymentCommand command) 
        => this.Ok(await mediator.Send(command));
        
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => this.Ok(await mediator.Send(new GetInvoicesQuery(page, pageSize)));

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
        => this.Ok(await mediator.Send(new GetInvoiceQuery(id)));

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => this.Ok(await mediator.Send(new GetPaymentsQuery(page, pageSize)));

    [HttpGet("reports/aging")]
    public async Task<IActionResult> GetAgingReport()
        => this.Ok(await mediator.Send(new GetAgingReportQuery()));
}

public record WriteOffInvoiceRequest(string Reason);

public record RecordPaymentRequest(decimal Amount, DateTime PaymentDate, Domain.PaymentMethod Method, string? ReferenceNo);