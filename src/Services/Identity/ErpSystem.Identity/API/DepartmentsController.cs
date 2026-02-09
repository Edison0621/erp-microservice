using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.API.Controllers;

[ApiController]
[Route("api/v1/identity/departments")]
public class DepartmentsController(IMediator mediator, IdentityReadDbContext readContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(new { DepartmentId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // For a tree structure, you might want a recursive DTO logic or just return flat list
        // returning flat list for now, Client can reconstruct tree via ParentId
        List<DepartmentReadModel> depts = await readContext.Departments.AsNoTracking().OrderBy(d => d.Order).ToListAsync();
        return this.Ok(depts);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveDepartmentCommand command)
    {
        if (id != command.DepartmentId) return this.BadRequest("Id mismatch");
        await mediator.Send(command);
        return this.NoContent();
    }
}
