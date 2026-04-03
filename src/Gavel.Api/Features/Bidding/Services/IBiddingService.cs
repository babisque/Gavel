using Gavel.Core.Domain.Lots;

namespace Gavel.Api.Features.Bidding.Services;

public interface IBiddingService
{
    Task<(PriceBreakdown Breakdown, DateTimeOffset? NewEndTime)> PlaceBidAsync(Guid lotId, Guid bidderId, decimal amount, string? sourceIp);
    Task<(PriceBreakdown Breakdown, DateTimeOffset? NewEndTime)> PlaceProxyBidAsync(Guid lotId, Guid bidderId, decimal maxAmount, string? sourceIp);
    Task<Lot?> GetLotForBiddingAsync(Guid lotId);
}
