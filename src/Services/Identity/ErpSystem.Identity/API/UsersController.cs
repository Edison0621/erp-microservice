using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.API;

[ApiController]
[Route("api/v1/identity/users")]
public class UsersController(IMediator mediator, IdentityReadDbContext readDb) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(RegisterUserCommand command)
    {
        Guid id = await mediator.Send(command);
        return this.Ok(id);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => this.Ok(await readDb.Users.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await readDb.Users.FindAsync(id));

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdateUserProfileCommand command)
    {
        if (id != command.UserId) return this.BadRequest();
        await mediator.Send(command);
        return this.NoContent();
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> Lock(Guid id, [FromBody] string reason)
    {
        // For demo, lock for 15 mins
        await mediator.Send(new LockUserCommand(id, reason, TimeSpan.FromMinutes(15)));
        return this.NoContent();
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        await mediator.Send(new UnlockUserCommand(id));
        return this.NoContent();
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] string roleCode)
    {
        await mediator.Send(new AssignRoleToUserCommand(id, roleCode));
        return this.NoContent();
    }
}
