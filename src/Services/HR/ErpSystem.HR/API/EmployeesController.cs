using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.HR.Application;

namespace ErpSystem.HR.API;

[ApiController]
[Route("api/v1/hr/employees")]
public class EmployeesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Hire(HireEmployeeCommand command) => this.Ok(await mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await mediator.Send(new GetEmployeeByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? fullName, [FromQuery] string? departmentId, [FromQuery] string? status, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new SearchEmployeesQuery(fullName, departmentId, status, page)));

    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> Transfer(Guid id, TransferEmployeeCommand command)
    {
        if (id != command.EmployeeId) return this.BadRequest();
        return this.Ok(await mediator.Send(command));
    }

    [HttpPost("{id}/promote")]
    public async Task<IActionResult> Promote(Guid id, PromoteEmployeeCommand command)
    {
        if (id != command.EmployeeId) return this.BadRequest();
        return this.Ok(await mediator.Send(command));
    }

    [HttpPost("{id}/terminate")]
    public async Task<IActionResult> Terminate(Guid id, TerminateEmployeeCommand command)
    {
        if (id != command.EmployeeId) return this.BadRequest();
        return this.Ok(await mediator.Send(command));
    }

    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetEvents(Guid id)
        =>
            this.Ok(await mediator.Send(new GetEmployeeEventsQuery(id)));
}
