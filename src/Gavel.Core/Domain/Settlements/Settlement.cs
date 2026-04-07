using Gavel.Core.Domain.Lots;

namespace Gavel.Core.Domain.Settlements;

public enum SettlementStatus
{
    PendingSignature,
    Signed,
    Paid,
    Overdue,
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
    DateTimeOffset paymentDeadline,
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
    public DateTimeOffset PaymentDeadline { get; init; } = paymentDeadline;
    public DateTimeOffset? PaidAt { get; private set; }
    public string? DigitalSignature { get; private set; }
    public string? SaleNoteUrl { get; private set; }
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
    /// Updates the Sale Note URL after it has been generated and stored.
    /// </summary>
    public void SetSaleNoteUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            throw new ArgumentException("The provided URL is not a well-formed absolute URI.", nameof(url));

        if (Status != SettlementStatus.Signed && Status != SettlementStatus.Paid)
            throw new InvalidOperationException("Sale Note can only be set for signed or paid settlements.");

        SaleNoteUrl = url;
    }

    /// <summary>
    /// Marks the settlement as paid.
    /// </summary>
    public void MarkAsPaid(DateTimeOffset paidAt)
    {
        if (Status != SettlementStatus.Signed && Status != SettlementStatus.PendingSignature && Status != SettlementStatus.Overdue)
            throw new InvalidOperationException($"Cannot mark settlement as paid from state {Status}.");

        Status = SettlementStatus.Paid;
        PaidAt = paidAt;
    }

    /// <summary>
    /// Marks the settlement as overdue due to payment default.
    /// </summary>
    public void MarkAsOverdue()
    {
        if (Status != SettlementStatus.PendingSignature && Status != SettlementStatus.Signed)
            throw new InvalidOperationException("Only pending or signed settlements can be marked as overdue.");

        Status = SettlementStatus.Overdue;
    }

    /// <summary>
    /// Cancels the settlement. If it is already signed, an administrative override is required.
    /// </summary>
    public void Cancel(string reason, bool isAdministrativeOverride = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if ((Status == SettlementStatus.Signed || Status == SettlementStatus.Paid) && !isAdministrativeOverride)
            throw new InvalidOperationException("A signed or paid settlement cannot be canceled without an administrative override.");

        if (Status == SettlementStatus.Canceled)
            throw new InvalidOperationException("Settlement is already canceled.");

        Status = SettlementStatus.Canceled;
        CancellationReason = reason;
    }

    private Settlement() : this(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, null!, DateTimeOffset.MinValue, DateTimeOffset.MinValue) { }
}
