using Gavel.Core.Domain.Lots;

namespace Gavel.Core.Domain.Settlements;

/// <summary>
/// Represents the final financial settlement of a lot arrematation.
/// This entity is immutable and serves as the primary record for invoicing and legal compliance.
/// </summary>
public class Settlement(
    Guid id,
    Guid lotId,
    Guid bidderId,
    Guid winningBidId,
    PriceBreakdown priceBreakdown,
    DateTimeOffset issuedAt)
{
    public Guid Id { get; init; } = id;
    public Guid LotId { get; init; } = lotId;
    public Guid BidderId { get; init; } = bidderId;
    public Guid WinningBidId { get; init; } = winningBidId;
    
    /// <summary>
    /// Financial breakdown including Hammer Price, Commission, and Fees.
    /// Encapsulated as a Complex Property in EF Core 10.
    /// </summary>
    public PriceBreakdown PriceBreakdown { get; init; } = priceBreakdown;
    
    public DateTimeOffset IssuedAt { get; init; } = issuedAt;

    private Settlement() : this(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, null!, DateTimeOffset.MinValue) { }
}
