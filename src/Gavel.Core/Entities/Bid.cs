namespace Gavel.Domain.Entities;

public class Bid
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime TimeStamp { get; set; }
    public Guid AuctionItemId { get; set; }
    public virtual AuctionItem AuctionItem { get; set; }
    public Guid BidderId { get; set; }
    public virtual ApplicationUser Bidder { get; set; }
}
