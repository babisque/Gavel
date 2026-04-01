namespace Gavel.Core.Domain.Auctions;

using Gavel.Core.Domain.Registration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Represents an Auction session, enforcing scheduling and participation rules 
/// as per Decree No. 21,981/1932.
/// </summary>
public class Auction
{
    private readonly List<Bidder> _registeredBidders = [];

    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset StartDateTime { get; private set; }
    public DateTimeOffset EndDateTime { get; private set; }
    public bool RequiresGuarantee { get; init; }

    public IReadOnlyCollection<Bidder> RegisteredBidders => _registeredBidders.AsReadOnly();

    /// <summary>
    /// Parameterless constructor required by EF Core 10.
    /// Hidden from public API to ensure valid object creation via the parameterized constructor.
    /// </summary>
    protected Auction() { }

    /// <summary>
    /// Initializes a new instance of the Auction entity with strict schedule validation.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="startDateTime">UTC start time.</param>
    /// <param name="endDateTime">UTC end time (must be after start time).</param>
    public Auction(Guid id, DateTimeOffset startDateTime, DateTimeOffset endDateTime)
    {
        if (endDateTime <= startDateTime)
            throw new ArgumentException("Auction end time must be strictly later than start time.");

        Id = id;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
    }

    /// <summary>
    /// Registers a bidder for this specific auction.
    /// Validates qualification status, mandatory guarantees, and prevents duplicate registration.
    /// </summary>
    /// <param name="bidder">The bidder to register.</param>
    /// <param name="hasPaidGuarantee">Indicates if the mandatory guarantee (if required) was confirmed.</param>
    public void RegisterBidder(Bidder bidder, bool hasPaidGuarantee = false)
    {
        ArgumentNullException.ThrowIfNull(bidder, nameof(bidder));

        // 1. Legal Qualification Check (Decree No. 21,981/1932)
        if (bidder.State != BidderState.Approved)
            throw new InvalidStateTransitionException($"Only approved bidders can register. Bidder {bidder.Id} is in state {bidder.State}.");

        // 2. Financial Guarantee Validation
        if (RequiresGuarantee && !hasPaidGuarantee)
        {
            throw new GuaranteeMissingException($"A mandatory guarantee deposit is required for auction {Id}.");
        }

        // 3. Fail-Fast Duplicity Check
        if (_registeredBidders.Any(b => b.Id == bidder.Id))
        {
            throw new InvalidOperationException($"Bidder {bidder.Id} is already registered for this auction.");
        }

        _registeredBidders.Add(bidder);
    }
}
