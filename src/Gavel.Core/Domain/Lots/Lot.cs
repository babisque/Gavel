using Gavel.Core.Domain.Auctions;
using System.Collections.ObjectModel;

namespace Gavel.Core.Domain.Lots;

public record Commission
{
    public decimal Rate { get; init; } = 0.05m;
    public decimal Calculate(decimal amount) => amount * Rate;
}

public record PriceBreakdown(
    decimal BidAmount, 
    decimal CommissionAmount, 
    decimal AdminFees, 
    decimal Total);

public class PublicNotice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTimeOffset AttachedAt { get; init; }

    internal PublicNotice() { }

    public PublicNotice(string url, string version, DateTimeOffset attachedAt)
    {
        Url = url;
        Version = version;
        AttachedAt = attachedAt;
    }
}

public class Photo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = string.Empty;
    public int Order { get; internal set; }

    internal Photo() { }

    public Photo(string url, int order)
    {
        Url = url;
        Order = order;
    }
}

public class Lot
{
    public Guid Id { get; init; }
    public Guid AuctionId { get; init; }
    public string Title { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }
    
    public decimal CurrentPrice 
    { 
        get; 
        private set 
        {
            if (State != LotState.Draft && value <= field)
                throw new InvalidOperationException("New bid must be higher than the current price.");

            if (value < StartingPrice) 
                throw new ArgumentException("Price cannot be lower than the starting price.");
            
            field = value;
        }
    }

    public LotState State { get; private set; } = LotState.Draft;
    public decimal AdminFees { get; private set; }
    
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    
    public TimeSpan SoftCloseWindow { get; set; } = TimeSpan.FromMinutes(3);

    public List<Photo> Photos { get; private set; } = [];
    public List<PublicNotice> NoticeHistory { get; private set; } = [];

    public PublicNotice? CurrentNotice => NoticeHistory.OrderByDescending(n => n.AttachedAt).FirstOrDefault();

    public Commission Commission { get; init; } = new();

    internal Lot() { }

    public Lot(Guid id, Guid auctionId, string title, decimal startingPrice)
    {
        if (startingPrice <= 0) throw new ArgumentException("Starting price must be greater than zero.");
        
        Id = id;
        AuctionId = auctionId;
        Title = title;
        StartingPrice = startingPrice;
        CurrentPrice = startingPrice;
    }

    public void AttachPublicNotice(string url, string version, DateTimeOffset attachedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        
        NoticeHistory.Add(new PublicNotice(url, version, attachedAt));
    }

    public void AddPhoto(string url, int? order = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        
        int targetOrder = order ?? (Photos.Count + 1);
        Photos.Add(new Photo(url, targetOrder));
        ReorderPhotos();
    }

    private void ReorderPhotos()
    {
        var ordered = Photos.OrderBy(p => p.Order).ThenBy(p => p.Url).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order = i + 1;
        }
    }

    public void Schedule(DateTimeOffset start, DateTimeOffset end)
    {
        if (State != LotState.Draft)
            throw new InvalidOperationException("Only lots in Draft can be scheduled.");

        if (start >= end)
            throw new ArgumentException("Start time must be before end time.");

        if (Photos.Count == 0)
            throw new InvalidOperationException("A lot must have at least one photo before being scheduled.");

        if (CurrentNotice == null)
            throw new InvalidOperationException("A lot must have a Public Notice (Edital) before being scheduled.");

        StartTime = start;
        EndTime = end;
        State = LotState.Scheduled;
    }

    public void OpenForBidding(DateTimeOffset now)
    {
        if (State != LotState.Scheduled)
            throw new InvalidOperationException("Only scheduled lots can be opened for bidding.");

        if (StartTime.HasValue && now < StartTime.Value)
            throw new InvalidOperationException("Cannot open lot before its scheduled start time.");

        State = LotState.Active;
    }

    public void PlaceBid(decimal amount, DateTimeOffset bidTime)
    {
        if (State != LotState.Active && State != LotState.Closing)
            throw new InvalidOperationException("Bids can only be placed when the lot is Active or in the Closing phase.");

        CurrentPrice = amount;

        if (EndTime.HasValue && EndTime.Value - bidTime < SoftCloseWindow)
        {
            EndTime = bidTime.Add(SoftCloseWindow);
            State = LotState.Closing;
        }
    }

    public void Close(DateTimeOffset now)
    {
        if (State != LotState.Active && State != LotState.Closing)
            throw new InvalidOperationException("Only active lots can be closed.");

        if (EndTime.HasValue && now < EndTime.Value)
            throw new InvalidOperationException("Cannot close lot before its scheduled end time.");

        State = LotState.Closed;
    }

    public void SubmitToConditional()
    {
        if (State != LotState.Closed)
            throw new InvalidOperationException("Only closed lots can be moved to conditional.");

        State = LotState.Conditional;
    }

    public void MarkAsSold()
    {
        if (State != LotState.Closed && State != LotState.Conditional)
            throw new InvalidOperationException("Only closed or conditional lots can be marked as sold.");

        State = LotState.Sold;
    }

    public void MarkAsUnsold()
    {
        if (State != LotState.Closed && State != LotState.Conditional)
            throw new InvalidOperationException("Only closed or conditional lots can be marked as unsold.");

        State = LotState.Unsold;
    }

    public PriceBreakdown GetPriceBreakdown()
    {
        var commissionAmount = Commission.Calculate(CurrentPrice);
        var total = CurrentPrice + commissionAmount + AdminFees;
        return new PriceBreakdown(CurrentPrice, commissionAmount, AdminFees, total);
    }

    public void SetAdminFees(decimal fees)
    {
        if (fees < 0) throw new ArgumentException("Admin fees cannot be negative.");
        AdminFees = fees;
    }
}
