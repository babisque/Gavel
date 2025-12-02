using Gavel.Application.Handlers.Auth.LoginUser;
using Gavel.Application.Handlers.Auth.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register-user")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand request)
    {
        var result = await mediator.Send(request);

        return Created(String.Empty, new { Id = result });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
    {
        var result = await mediator.Send(request);

        return Ok(result);
    }
}