namespace Gavel.Core.Domain.Bidding;

/// <summary>
/// Represents an automatic bidding instruction (Proxy Bid).
/// The system will use this to automatically outbid competitors up to the MaxAmount.
/// </summary>
public class ProxyBid(Guid id, Guid lotId, Guid bidderId, decimal maxAmount, DateTimeOffset createdAt)
{
    public Guid Id { get; init; } = id;

    public Guid LotId { get; init; } = lotId;
    public Guid BidderId { get; init; } = bidderId;
    
    public decimal MaxAmount { get; private set; } = decimal.Round(maxAmount, 2);
    public DateTimeOffset CreatedAt { get; init; } = createdAt;
    public byte[] RowVersion { get; init; } = [];

    private ProxyBid() : this(Guid.Empty, Guid.Empty, Guid.Empty, 0, DateTimeOffset.MinValue) { }

    public void UpdateMaxAmount(decimal newMaxAmount)
    {
        if (newMaxAmount <= MaxAmount)
            throw new InvalidOperationException("New maximum amount must be greater than the current one.");
            
        MaxAmount = decimal.Round(newMaxAmount, 2);
    }
}
