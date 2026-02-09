using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;

namespace ErpSystem.Identity.API.Controllers;

[ApiController]
[Route("api/v1/identity/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        Guid uid = await mediator.Send(command);
        return this.Ok(new { UserId = uid });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        try
        {
            string token = await mediator.Send(command);
            return this.Ok(new { Token = token });
        }
        catch (Exception ex)
        {
            return this.Unauthorized(ex.Message);
        }
    }
}
