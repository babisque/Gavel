using Gavel.Domain.Common;
using Gavel.Domain.Enums;
using Gavel.Domain.Events;
using Gavel.Domain.Exceptions;
using Gavel.Domain.ValueObjects;

namespace Gavel.Domain.Entities;

public class AuctionItem : BaseEntity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money InitialPrice { get; private set; }
    public Money CurrentPrice { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Pending;
    public byte[] RowVersion { get; private set; }

    private readonly List<Bid> _bids = [];
    public virtual IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    public AuctionItem() { }

    public AuctionItem(string name, string description, Money initialPrice, DateTime endTime)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        InitialPrice = initialPrice;
        CurrentPrice = initialPrice;
        StartTime = DateTime.UtcNow;
        EndTime = endTime;
        Status = AuctionStatus.Active;
        
        AddDomainEvent(new AuctionItemCreatedEvent(Id, EndTime));
    }

    public void PlaceBid(Bid bid)
    {
        if (Status != AuctionStatus.Active)
            throw new ConflictException("Auction is not active.");
        
        if (EndTime < DateTime.UtcNow)
            throw new ConflictException("Auction has already ended.");

        var minBidAmount = new Money(CurrentPrice.Amount * 1.05m, CurrentPrice.Currency);
        if (!bid.Amount.IsGreaterThan(minBidAmount))
            throw new ConflictException($"Bid amount must be at least {minBidAmount:C} (5% higher than current price).");
        
        var lastBid = _bids.MaxBy(b => b.Amount);
        if (lastBid is not null && lastBid.BidderId == bid.BidderId)
            throw new ConflictException("You are already the highest bidder.");
        
        CurrentPrice = bid.Amount;
        _bids.Add(bid);
        
        AddDomainEvent(new BidPlacedEvent(bid));
    }

    public void CloseAuction()
    {
        Status = AuctionStatus.Active;
    }
}
