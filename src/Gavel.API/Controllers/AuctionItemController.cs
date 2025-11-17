using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gavel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionItemController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuctionItems([FromQuery] GetAuctionItemsQuery request)
    {
        var res = await mediator.Send(request);
        return Ok(res);
    }
}
