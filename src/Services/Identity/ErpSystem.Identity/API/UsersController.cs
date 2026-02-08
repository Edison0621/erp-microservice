using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IdentityReadDbContext _readDb;

    public UsersController(IMediator mediator, IdentityReadDbContext readDb)
    {
        _mediator = mediator;
        _readDb = readDb;
    }

    [HttpPost]
    public async Task<IActionResult> Create(RegisterUserCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _readDb.Users.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _readDb.Users.FindAsync(id));

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdateUserProfileCommand command)
    {
        if (id != command.UserId) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> Lock(Guid id, [FromBody] string reason)
    {
        // For demo, lock for 15 mins
        await _mediator.Send(new LockUserCommand(id, reason, TimeSpan.FromMinutes(15)));
        return NoContent();
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        await _mediator.Send(new UnlockUserCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] string roleCode)
    {
        await _mediator.Send(new AssignRoleToUserCommand(id, roleCode));
        return NoContent();
    }
}
