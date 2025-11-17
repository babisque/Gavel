namespace Gavel.Domain.Entities;

public class Bid
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime TimeStamp { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public Guid AuctionItemId { get; set; }
    public virtual AuctionItem AuctionItem { get; set; }
}
