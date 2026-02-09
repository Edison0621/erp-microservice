using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ErpSystem.Identity.Domain;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/roles")]
public class RolesController(IMediator mediator, IdentityReadDbContext readDb) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => this.Ok(await readDb.Roles.ToListAsync());

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] string permissionCode)
    {
        await mediator.Send(new AssignRolePermissionCommand(id, permissionCode));
        return this.NoContent();
    }

    [HttpPost("{id}/data-permissions")]
    public async Task<IActionResult> ConfigureDataPermission(Guid id, [FromBody] ConfigureDataPermissionRequest request)
    {
        if (id != request.RoleId) return this.BadRequest();
        await mediator.Send(new ConfigureRoleDataPermissionCommand(request.RoleId, request.DataDomain, request.ScopeType, request.AllowedIds));
        return this.NoContent();
    }
}

public record ConfigureDataPermissionRequest(Guid RoleId, string DataDomain, ScopeType ScopeType, List<string> AllowedIds);

[ApiController]
[Route("api/v1/identity/positions")]
public class PositionsController(IMediator mediator, IdentityReadDbContext readDb) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePositionCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => this.Ok(await readDb.Positions.ToListAsync());
}
