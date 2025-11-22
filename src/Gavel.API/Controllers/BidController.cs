using System.Security.Claims;
using Gavel.Application.Handlers.Bids.PlaceBid;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceBid([FromBody] PlaceBidCommand request)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userClaim is null | !Guid.TryParse(userClaim.Value, out Guid userId)) return Unauthorized();
        
        request.BidderId = userId;
        
        await mediator.Send(request);
        return Ok(new { message = "Bid placed successfully." });
    }
}