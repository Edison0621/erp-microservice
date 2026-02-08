using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ErpSystem.Identity.Domain;
using System.Collections.Generic;
using System.Linq;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/roles")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IdentityReadDbContext _readDb;

    public RolesController(IMediator mediator, IdentityReadDbContext readDb)
    {
        _mediator = mediator;
        _readDb = readDb;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _readDb.Roles.ToListAsync());

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] string permissionCode)
    {
        await _mediator.Send(new AssignRolePermissionCommand(id, permissionCode));
        return NoContent();
    }

    [HttpPost("{id}/data-permissions")]
    public async Task<IActionResult> ConfigureDataPermission(Guid id, [FromBody] ConfigureDataPermissionRequest request)
    {
        if (id != request.RoleId) return BadRequest();
        await _mediator.Send(new ConfigureRoleDataPermissionCommand(request.RoleId, request.DataDomain, request.ScopeType, request.AllowedIds));
        return NoContent();
    }
}

public record ConfigureDataPermissionRequest(Guid RoleId, string DataDomain, ScopeType ScopeType, List<string> AllowedIds);


[ApiController]
[Route("api/v1/identity/positions")]
public class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IdentityReadDbContext _readDb;

    public PositionsController(IMediator mediator, IdentityReadDbContext readDb)
    {
        _mediator = mediator;
        _readDb = readDb;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePositionCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _readDb.Positions.ToListAsync());
}
