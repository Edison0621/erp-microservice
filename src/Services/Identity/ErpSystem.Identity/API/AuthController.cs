using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Application;

namespace ErpSystem.Identity.API.Controllers;

[ApiController]
[Route("api/v1/identity/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var uid = await _mediator.Send(command);
        return Ok(new { UserId = uid });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        try
        {
            var token = await _mediator.Send(command);
            return Ok(new { Token = token });
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
