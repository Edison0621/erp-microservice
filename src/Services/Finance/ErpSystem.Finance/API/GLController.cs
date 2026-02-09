using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Infrastructure;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/gl")]
public class GlController(IMediator mediator) : ControllerBase
{
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] DefineAccountCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.CreatedAtAction(nameof(this.GetAccounts), new { id }, id);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        List<AccountReadModel> result = await mediator.Send(new GetChartOfAccountsQuery());
        return this.Ok(result);
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(new { JournalEntryId = id }); // Return OK for draft creation
    }

    [HttpPost("journal-entries/{id}/post")]
    public async Task<IActionResult> PostJournalEntry(Guid id)
    {
        try
        {
            await mediator.Send(new PostJournalEntryCommand(id));
            return this.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
    }

    [HttpGet("journal-entries/{id}")]
    public async Task<IActionResult> GetJournalEntry(Guid id)
    {
        JournalEntryDetailDto? result = await mediator.Send(new GetJournalEntryQuery(id));
        if (result == null) return this.NotFound();
        return this.Ok(result);
    }

    [HttpGet("reports/trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? asOfDate)
    {
        List<TrialBalanceLineDto> result = await mediator.Send(new GetTrialBalanceQuery(asOfDate));
        return this.Ok(result);
    }

    [HttpPost("periods")]
    public async Task<IActionResult> DefinePeriod([FromBody] DefineFinancialPeriodCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(new { PeriodId = id });
    }

    [HttpPost("periods/{id}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id)
    {
        await mediator.Send(new CloseFinancialPeriodCommand(id));
        return this.Ok();
    }
}
