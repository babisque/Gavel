using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidCommand : IRequest
{
    public Guid AuctionItemId { get; set; }
    public string BidderName { get; set; }
    public decimal Amount { get; set; }
}