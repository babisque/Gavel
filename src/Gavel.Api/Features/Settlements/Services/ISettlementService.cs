using Gavel.Core.Domain.Settlements;

namespace Gavel.Api.Features.Settlements.Services;

public interface ISettlementService
{
    Task<Settlement?> GetSettlementAsync(Guid id);
    Task<List<Settlement>> GetBidderSettlementsAsync(Guid bidderId);
    Task ProcessExpiredLotsAsync(CancellationToken ct);
}
