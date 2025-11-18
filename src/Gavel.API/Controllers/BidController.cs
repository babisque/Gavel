using Gavel.API.Contracts;
using Gavel.Application.Handlers.Bid.PlaceBid;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PlaceBid([FromBody] PlaceBidCommand request)
    {
        await mediator.Send(request);
        var response = ApiResponseFactory.Success("Bid placed successfully.");
        return Ok(response);
    }
}