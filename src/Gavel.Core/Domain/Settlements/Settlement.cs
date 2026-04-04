using Gavel.Core.Domain.Lots;

namespace Gavel.Core.Domain.Settlements;

public enum SettlementStatus
{
    PendingSignature,
    Signed,
    Canceled
}

/// <summary>
/// Represents the final financial settlement of a lot arrematation.
/// This entity follows a Rich Domain Model pattern, encapsulating state transitions 
/// and ensuring the integrity of the legal and financial record.
/// </summary>
public class Settlement(
    Guid id,
    Guid lotId,
    Guid bidderId,
    Guid winningBidId,
    PriceBreakdown priceBreakdown,
    DateTimeOffset issuedAt,
    SettlementStatus status = SettlementStatus.PendingSignature)
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
    public string? DigitalSignature { get; private set; }
    public SettlementStatus Status { get; private set; } = status;
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Applies a digital signature to the settlement, transitioning it to the Signed state.
    /// </summary>
    /// <param name="signature">The cryptographic hash or mark.</param>
    public void ApplySignature(string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        if (Status != SettlementStatus.PendingSignature)
            throw new InvalidOperationException("Only settlements in PendingSignature state can be signed.");
            
        DigitalSignature = signature;
        Status = SettlementStatus.Signed;
    }

    /// <summary>
    /// Cancels the settlement. If it is already signed, an administrative override is required.
    /// </summary>
    public void Cancel(string reason, bool isAdministrativeOverride = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == SettlementStatus.Signed && !isAdministrativeOverride)
            throw new InvalidOperationException("A signed settlement cannot be canceled without an administrative override.");

        if (Status == SettlementStatus.Canceled)
            throw new InvalidOperationException("Settlement is already canceled.");

        Status = SettlementStatus.Canceled;
        CancellationReason = reason;
    }

    private Settlement() : this(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, null!, DateTimeOffset.MinValue) { }
}
