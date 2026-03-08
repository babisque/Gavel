using System.Text.Json.Serialization;
using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidCommand : IRequest
{
    public Guid AuctionItemId { get; set; }
    public decimal Amount { get; set; }
    
    [JsonIgnore]
    public Guid BidderId { get; set; }
}