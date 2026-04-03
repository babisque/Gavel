namespace Gavel.Core.Domain.Bidding;

/// <summary>
/// Represents an immutable record of a bid placed on a lot.
/// This entity is append-only and serves as the legal audit trail.
/// </summary>
public class Bid(Guid id, Guid lotId, Guid bidderId, decimal amount, DateTimeOffset timestamp, string sourceIP)
{
    public Guid Id { get; init; } = id;
    public Guid LotId { get; init; } = lotId;
    public Guid BidderId { get; init; } = bidderId;
    public decimal Amount { get; init; } = amount;
    public DateTimeOffset Timestamp { get; init; } = timestamp;
    public string SourceIP { get; init; } = sourceIP;

    // EF Core constructor
    private Bid() : this(Guid.Empty, Guid.Empty, Guid.Empty, 0, DateTimeOffset.MinValue, string.Empty) { }
}
