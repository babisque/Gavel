using Gavel.Application.Handlers.Auth.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand request)
    {
        var result = await mediator.Send(request);

        return Created(String.Empty, new { Id = result });
    }
}