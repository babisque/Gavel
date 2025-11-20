using Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;
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

        return Ok(new
        {
            Items = items,
            Meta = new
            {
                Page = request.Page,
                PageSize = request.Size,
                TotalRecords = totalCount
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuctionItemById([FromRoute] Guid id)
    {
        var request = new GetAuctionItemByIdQuery { Id = id };
        var item = await mediator.Send(request);
        return Ok(item);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAuctionItem([FromBody] CreateAuctionItemCommand request)
    {
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetAuctionItemById), new { id }, new { id });
    }
}
