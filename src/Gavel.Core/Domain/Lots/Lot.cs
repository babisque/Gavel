using Gavel.Core.Domain.Auctions;
using System.Collections.ObjectModel;

namespace Gavel.Core.Domain.Lots;

/// <summary>
/// Represents the mandatory 5% Auctioneer Commission as per Decree No. 21,981/1932.
/// </summary>
public record Commission
{
    public decimal Rate { get; } = 0.05m;
    public decimal Calculate(decimal amount) => amount * Rate;
}

/// <summary>
/// Value Object for price transparency, detailing the composition of the total amount payable.
/// </summary>
public record PriceBreakdown(
    decimal BidAmount, 
    decimal CommissionAmount, 
    decimal AdminFees, 
    decimal Total);

public record PublicNotice(string Url, string Version, DateTimeOffset AttachedAt);

public class Photo(string url, int order)
{
    public string Url { get; } = url;
    public int Order { get; internal set; } = order;
}

/// <summary>
/// Represents a Lot in an auction, managing its lifecycle and bidding engine
/// according to Brazilian regulatory framework.
/// </summary>
public class Lot
{
    private readonly List<Photo> _photos = [];
    private readonly List<PublicNotice> _noticeHistory = [];

    public Guid Id { get; init; }
    public Guid AuctionId { get; init; }
    public string Title { get; private set; }
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
    
    /// <summary>
    /// Configurable window for Soft Close (Time Extension). 
    /// Defaults to 3 minutes as per standard industry practice and Decree 21,981/32.
    /// </summary>
    public TimeSpan SoftCloseWindow { get; set; } = TimeSpan.FromMinutes(3);

    public IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();
    public PublicNotice? CurrentNotice => _noticeHistory.LastOrDefault();
    public IReadOnlyCollection<PublicNotice> NoticeHistory => _noticeHistory.AsReadOnly();

    public Commission Commission { get; } = new();

    public Lot(Guid id, Guid auctionId, string title, decimal startingPrice)
    {
        if (startingPrice <= 0) throw new ArgumentException("Starting price must be greater than zero.");
        
        Id = id;
        AuctionId = auctionId;
        Title = title;
        StartingPrice = startingPrice;
        CurrentPrice = startingPrice;
    }

    /// <summary>
    /// Attaches a Public Notice (Edital) to the lot. Mandatory for publication.
    /// Supports versioning for legal auditability.
    /// </summary>
    public void AttachPublicNotice(string url, string version, DateTimeOffset attachedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        
        _noticeHistory.Add(new PublicNotice(url, version, attachedAt));
    }

    /// <summary>
    /// Adds a photo to the lot's catalog with optional manual ordering.
    /// Reorders existing photos to maintain sequence integrity.
    /// </summary>
    public void AddPhoto(string url, int? order = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        
        int targetOrder = order ?? (_photos.Count + 1);
        _photos.Add(new Photo(url, targetOrder));
        ReorderPhotos();
    }

    /// <summary>
    /// Ensures photo sequence is consistent and gap-free.
    /// </summary>
    private void ReorderPhotos()
    {
        var ordered = _photos.OrderBy(p => p.Order).ThenBy(p => p.Url).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order = i + 1;
        }
        _photos.Clear();
        _photos.AddRange(ordered);
    }

    /// <summary>
    /// Schedules the lot for a public session.
    /// Validates mandatory requirements: photos, starting price, and public notice.
    /// </summary>
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

    /// <summary>
    /// Opens the lot for real-time bidding.
    /// Marks the start of the Public Session phase.
    /// </summary>
    public void OpenForBidding(DateTimeOffset now)
    {
        if (State != LotState.Scheduled)
            throw new InvalidOperationException("Only scheduled lots can be opened for bidding.");

        if (StartTime.HasValue && now < StartTime.Value)
            throw new InvalidOperationException("Cannot open lot before its scheduled start time.");

        State = LotState.Active;
    }

    /// <summary>
    /// Processes a new bid, validating state and price increments.
    /// Implements Soft Close (Time Extension) logic using the configurable window.
    /// </summary>
    public void PlaceBid(decimal amount, DateTimeOffset bidTime)
    {
        if (State != LotState.Active && State != LotState.Closing)
            throw new InvalidOperationException("Bids can only be placed when the lot is Active or in the Closing phase.");

        // Setter of CurrentPrice handles the 'amount > CurrentPrice' validation via 'field' keyword
        CurrentPrice = amount;

        // Soft Close Logic: If a bid is received in the last minutes defined by SoftCloseWindow, extend it.
        if (EndTime.HasValue && EndTime.Value - bidTime < SoftCloseWindow)
        {
            EndTime = bidTime.Add(SoftCloseWindow);
            State = LotState.Closing;
        }
    }

    /// <summary>
    /// Closes the bidding session.
    /// </summary>
    public void Close(DateTimeOffset now)
    {
        if (State != LotState.Active && State != LotState.Closing)
            throw new InvalidOperationException("Only active lots can be closed.");

        if (EndTime.HasValue && now < EndTime.Value)
            throw new InvalidOperationException("Cannot close lot before its scheduled end time.");

        State = LotState.Closed;
    }

    /// <summary>
    /// Submits the lot to the consignor for approval when the reserve price is not met.
    /// </summary>
    public void SubmitToConditional()
    {
        if (State != LotState.Closed)
            throw new InvalidOperationException("Only closed lots can be moved to conditional.");

        State = LotState.Conditional;
    }

    /// <summary>
    /// Finalizes the sale after payment confirmation.
    /// </summary>
    public void MarkAsSold()
    {
        if (State != LotState.Closed && State != LotState.Conditional)
            throw new InvalidOperationException("Only closed or conditional lots can be marked as sold.");

        State = LotState.Sold;
    }

    /// <summary>
    /// Marks the lot as unsold if no bids were placed or conditional sale was rejected.
    /// </summary>
    public void MarkAsUnsold()
    {
        if (State != LotState.Closed && State != LotState.Conditional)
            throw new InvalidOperationException("Only closed or conditional lots can be marked as unsold.");

        State = LotState.Unsold;
    }

    /// <summary>
    /// Returns a detailed breakdown of the total price, ensuring financial transparency.
    /// </summary>
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
