using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidCommand : IRequest
{
    public Guid AuctionItemId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
}