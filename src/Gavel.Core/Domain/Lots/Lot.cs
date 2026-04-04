using Gavel.Core.Domain.Auctions;

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

public class PublicNotice(string url, string version, DateTimeOffset attachedAt)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = url;
    public string Version { get; init; } = version;
    public DateTimeOffset AttachedAt { get; init; } = attachedAt;

    // EF Core constructor
    private PublicNotice() : this(string.Empty, string.Empty, DateTimeOffset.MinValue) { }
}

public class Photo(string url, int order)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = url;
    public int Order { get; internal set; } = order;

    // EF Core constructor
    private Photo() : this(string.Empty, 0) { }
}

public class Lot
{
    private readonly List<Photo> _photos = [];
    private readonly List<PublicNotice> _noticeHistory = [];

    public Guid Id { get; init; }
    public Guid AuctionId { get; init; }
    public string Title { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }
    public decimal? ReservePrice { get; private set; }
    public decimal MinimumIncrement { get; private set; }
    public byte[] RowVersion { get; init; } = [];
    
    public decimal CurrentPrice 
    { 
        get; 
        private set 
        {
            if (State != LotState.Draft)
            {
                if (CurrentBidderId != null && value < field + MinimumIncrement)
                    throw new InvalidOperationException($"New bid must be at least {field + MinimumIncrement}.");
                
                if (CurrentBidderId == null && value < field)
                    throw new InvalidOperationException("First bid must be at least the starting price.");
            }

            if (value < StartingPrice) 
                throw new ArgumentException("Price cannot be lower than the starting price.");
            
            field = value;
        }
    }

    public LotState State { get; private set; } = LotState.Draft;
    public decimal AdminFees { get; private set; }
    public Guid? CurrentBidderId { get; private set; }
    
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    
    public TimeSpan SoftCloseWindow { get; set; } = TimeSpan.FromMinutes(3);

    public IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<PublicNotice> NoticeHistory => _noticeHistory.AsReadOnly();

    public PublicNotice? CurrentNotice => _noticeHistory.OrderByDescending(n => n.AttachedAt).FirstOrDefault();

    public Commission Commission { get; init; } = new();

    internal Lot() { }

    public Lot(Guid id, Guid auctionId, string title, decimal startingPrice, decimal minimumIncrement = 1.00m)
    {
        if (startingPrice <= 0) throw new ArgumentException("Starting price must be greater than zero.");
        if (minimumIncrement <= 0) throw new ArgumentException("Minimum increment must be greater than zero.");
        
        Id = id;
        AuctionId = auctionId;
        Title = title;
        StartingPrice = startingPrice;
        CurrentPrice = startingPrice;
        MinimumIncrement = minimumIncrement;
    }

    public void AttachPublicNotice(string url, string version, DateTimeOffset attachedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        
        _noticeHistory.Add(new PublicNotice(url, version, attachedAt));
    }

    public void AddPhoto(string url, int? order = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        
        int targetOrder = order ?? (_photos.Count + 1);
        _photos.Add(new Photo(url, targetOrder));
        ReorderPhotos();
    }

    private void ReorderPhotos()
    {
        _photos.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0) return orderComparison;
            return string.Compare(x.Url, y.Url, StringComparison.OrdinalIgnoreCase);
        });

        for (int i = 0; i < _photos.Count; i++)
        {
            _photos[i].Order = i + 1;
        }
    }

    public void Schedule(DateTimeOffset start, DateTimeOffset end)
    {
        if (State != LotState.Draft)
            throw new InvalidOperationException("Only lots in Draft can be scheduled.");

        if (start >= end)
            throw new ArgumentException("Start time must be before end time.");

        if (_photos.Count == 0)
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

    public void PlaceBid(Guid bidderId, decimal amount, DateTimeOffset bidTime)
    {
        if (State != LotState.Active && State != LotState.Closing)
            throw new InvalidOperationException("Bids can only be placed when the lot is Active or in the Closing phase.");

        CurrentPrice = amount;
        CurrentBidderId = bidderId;

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

    public void SetReservePrice(decimal? price)
    {
        if (State != LotState.Draft && State != LotState.Scheduled)
            throw new InvalidOperationException("Reserve price can only be set in Draft or Scheduled state.");

        if (price.HasValue)
        {
            if (price.Value < 0) throw new ArgumentException("Reserve price cannot be negative.");
            if (price.Value < StartingPrice) throw new ArgumentException("Reserve price cannot be lower than starting price.");
        }
        
        ReservePrice = price;
    }
}
