using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Bidding;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Api.Features.Bidding.Services;

public class BiddingService(
    GavelDbContext context, 
    TimeProvider timeProvider,
    IAuditLogger auditLogger) : IBiddingService
{
    public async Task<(PriceBreakdown Breakdown, DateTimeOffset? NewEndTime)> PlaceBidAsync(Guid lotId, Guid bidderId, decimal amount, string? sourceIp)
    {
        var lot = await GetLotForBiddingOrThrowAsync(lotId);
        var now = timeProvider.GetUtcNow();

        try
        {
            var activeProxyBids = await context.ProxyBids
                .Where(p => p.LotId == lotId)
                .ToListAsync();

            var engine = new BiddingEngine();
            var result = engine.ProcessManualBid(lot, bidderId, amount, now, activeProxyBids, sourceIp);

            context.Bids.Add(result.WinningBid);

            await context.SaveChangesAsync();

            return (lot.GetPriceBreakdown(), result.NewEndTime);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            await auditLogger.LogAsync(new AuditRecord(
                bidderId,
                "BidRejected",
                now,
                $"Lot: {lotId}, Amount: {amount}, Reason: {ex.Message}, IP: {sourceIp}"
            ));
            throw;
        }
    }

    public async Task<(PriceBreakdown Breakdown, DateTimeOffset? NewEndTime)> PlaceProxyBidAsync(Guid lotId, Guid bidderId, decimal maxAmount, string? sourceIp)
    {
        var lot = await GetLotForBiddingOrThrowAsync(lotId);
        var now = timeProvider.GetUtcNow();

        try
        {
            var activeProxyBids = await context.ProxyBids
                .Where(p => p.LotId == lotId)
                .ToListAsync();

            var newProxy = new ProxyBid(Guid.NewGuid(), lotId, bidderId, maxAmount, now);
            
            var engine = new BiddingEngine();
            var result = engine.ProcessProxyBid(lot, newProxy, now, activeProxyBids, sourceIp);

            context.ProxyBids.Add(newProxy);
            context.Bids.Add(result.WinningBid);

            await context.SaveChangesAsync();

            return (lot.GetPriceBreakdown(), result.NewEndTime);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            await auditLogger.LogAsync(new AuditRecord(
                bidderId,
                "ProxyBidRejected",
                now,
                $"Lot: {lotId}, MaxAmount: {maxAmount}, Reason: {ex.Message}, IP: {sourceIp}"
            ));
            throw;
        }
    }

    public async Task<Lot?> GetLotForBiddingAsync(Guid lotId)
    {
        return await context.Lots
            .FirstOrDefaultAsync(l => l.Id == lotId);
    }

    private async Task<Lot> GetLotForBiddingOrThrowAsync(Guid lotId)
    {
        return await GetLotForBiddingAsync(lotId) 
               ?? throw new KeyNotFoundException($"Lot {lotId} not found.");
    }
}
