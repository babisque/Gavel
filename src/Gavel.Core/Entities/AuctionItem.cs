using System.Net.NetworkInformation;
using Gavel.Domain.Common;
using Gavel.Domain.Enums;
using Gavel.Domain.Events;
using Gavel.Domain.Exceptions;

namespace Gavel.Domain.Entities;

public class AuctionItem : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal InitialPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Pending;
    public byte[] RowVersion { get; set; }
    private readonly List<Bid> _bids = [];
    public virtual IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    public void PlaceBid(Bid bid)
    {
        if (Status != AuctionStatus.Active)
            throw new ConflictException("Auction is not active.");
        
        if (EndTime < DateTime.UtcNow)
            throw new ConflictException("Auction has already ended.");
        
        if (bid.Amount <= CurrentPrice)
            throw new ConflictException("Bid amount must be higher than the current price.");
        
        CurrentPrice = bid.Amount;
        _bids.Add(bid);
        
        AddDomainEvent(new BidPlacedEvent(bid));
    }
}
