using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.API.Controllers;

[ApiController]
[Route("api/v1/identity/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IdentityReadDbContext _readContext;

    public DepartmentsController(IMediator mediator, IdentityReadDbContext readContext)
    {
        _mediator = mediator;
        _readContext = readContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { DepartmentId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // For a tree structure, you might want a recursive DTO logic or just return flat list
        // returning flat list for now, Client can reconstruct tree via ParentId
        var depts = await _readContext.Departments.AsNoTracking().OrderBy(d => d.Order).ToListAsync();
        return Ok(depts);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveDepartmentCommand command)
    {
        if (id != command.DepartmentId) return BadRequest("Id mismatch");
        await _mediator.Send(command);
        return NoContent();
    }
}
