using Gavel.API.Contracts;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;
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
        var (items, totalCount) = await mediator.Send(request);

        var meta = new Meta(request.Page, request.Size, totalCount);
        var response = ApiResponseFactory.Success(items, meta);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuctionItemById([FromRoute] Guid id)
    {
        var request = new GetAuctionItemByIdQuery { Id = id };
        var item = await mediator.Send(request);
        var response = ApiResponseFactory.Success(item);
        return Ok(response);
    }
}
