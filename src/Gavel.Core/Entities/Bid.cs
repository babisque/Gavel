using Gavel.Domain.ValueObjects;

namespace Gavel.Domain.Entities;

public class Bid
{
    public Guid Id { get; private set; }
    public Money Amount { get; private set; }
    public DateTime TimeStamp { get; private set; }
    public Guid AuctionItemId { get; private set; }
    public virtual AuctionItem AuctionItem { get; private set; }
    public Guid BidderId { get; private set; }
    public virtual ApplicationUser Bidder { get; private set; }

    public Bid() { }

    public Bid(Money amount, Guid auctionItemId, Guid bidderId)
    {
        Id = Guid.NewGuid();
        Amount = amount;
        TimeStamp = DateTime.UtcNow;
        AuctionItemId = auctionItemId;
        BidderId = bidderId;
    }
}
