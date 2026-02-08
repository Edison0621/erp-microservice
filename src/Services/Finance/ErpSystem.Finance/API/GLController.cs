using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Domain;

namespace ErpSystem.Finance.API;

[ApiController]
[Route("api/gl")]
public class GLController : ControllerBase
{
    private readonly IMediator _mediator;

    public GLController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] DefineAccountCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAccounts), new { id }, id);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var result = await _mediator.Send(new GetChartOfAccountsQuery());
        return Ok(result);
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { JournalEntryId = id }); // Return OK for draft creation
    }

    [HttpPost("journal-entries/{id}/post")]
    public async Task<IActionResult> PostJournalEntry(Guid id)
    {
        try
        {
            await _mediator.Send(new PostJournalEntryCommand(id));
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("journal-entries/{id}")]
    public async Task<IActionResult> GetJournalEntry(Guid id)
    {
        var result = await _mediator.Send(new GetJournalEntryQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("reports/trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? asOfDate)
    {
        var result = await _mediator.Send(new GetTrialBalanceQuery(asOfDate));
        return Ok(result);
    }

    [HttpPost("periods")]
    public async Task<IActionResult> DefinePeriod([FromBody] DefineFinancialPeriodCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { PeriodId = id });
    }

    [HttpPost("periods/{id}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id)
    {
        await _mediator.Send(new CloseFinancialPeriodCommand(id));
        return Ok();
    }
}
