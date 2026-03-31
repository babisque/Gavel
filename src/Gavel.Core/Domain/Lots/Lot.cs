using Gavel.Core.Domain.Auctions;

namespace Gavel.Core.Domain.Lots;

public class Lot(
    Guid id, 
    Guid auctionId, 
    string title, 
    decimal startingPrice)
{
    public Guid Id { get; init; } = id;
    public Guid AuctionId { get; init; } = auctionId;
    public string Title { get; init; } = title;
    public decimal StartingPrice { get; init; } = startingPrice;
    
    public decimal CurrentPrice { 
        get; 
        private set {
            if (value < StartingPrice) throw new ArgumentException("Bid must be at least starting price");
            field = value;
        }
    } = startingPrice;

    public LotState State { 
        get; 
        private set {
            field = value;
        }
    } = LotState.Draft;
    
    public decimal CommissionRate { get; } = 0.05m;
    public decimal AdminFees { get; init; } = 0.00m;
    
    public DateTimeOffset EndTime { get; private set; }

    public void SetEndTime(DateTimeOffset endTime) => EndTime = endTime;

    public void TransitionTo(LotState newState)
    {
        if (State == LotState.Draft && newState == LotState.Active)
            throw new InvalidOperationException("Cannot transition from Draft directly to Active. Must be Scheduled first.");
        
        State = newState;
    }

    public decimal CalculateTotalPrice()
    {
        var commission = CurrentPrice * CommissionRate;
        return CurrentPrice + commission + AdminFees;
    }

    public void PlaceBid(decimal amount, DateTimeOffset bidTime, TimeProvider timeProvider)
    {
        CurrentPrice = amount;

        if (EndTime - bidTime < TimeSpan.FromMinutes(3))
            EndTime = bidTime.AddMinutes(3);
    }
}
