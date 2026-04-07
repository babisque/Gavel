using System.Text.Json;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Domain.Services;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gavel.Api.Features.Settlements.Services;

public class SettlementService(
    GavelDbContext context,
    TimeProvider timeProvider,
    IBusinessDayCalculator businessDayCalculator,
    IOptions<LotClosingOptions> options,
    ILogger<SettlementService> logger) : ISettlementService
{
    private readonly LotClosingOptions _options = options.Value;

    public async Task<Settlement?> GetSettlementAsync(Guid id)
    {
        return await context.Settlements
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Settlement>> GetBidderSettlementsAsync(Guid bidderId)
    {
        return await context.Settlements
            .AsNoTracking()
            .Where(s => s.BidderId == bidderId)
            .OrderByDescending(s => s.IssuedAt)
            .ToListAsync();
    }

    public async Task ProcessExpiredLotsAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        int processedCount = 0;

        while (processedCount < _options.BatchSize && !ct.IsCancellationRequested)
        {
            var processed = await ProcessNextExpiredLotAsync(now, ct);
            if (!processed) break;
            processedCount++;
        }
        
        if (processedCount > 0)
        {
            logger.LogInformation("Settlement Engine processed {Count} expired lots.", processedCount);
        }
    }

    internal async Task<bool> ProcessNextExpiredLotAsync(DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var lotQuery = context.Database.IsNpgsql()
                ? @"SELECT * FROM ""Lots"" WHERE (""State"" = 'Active' OR ""State"" = 'Closing') AND ""EndTime"" < {0} FOR UPDATE SKIP LOCKED LIMIT 1"
                : @"SELECT * FROM ""Lots"" WHERE (""State"" = 'Active' OR ""State"" = 'Closing') AND ""EndTime"" < {0} LIMIT 1";

            var lot = await context.Lots
                .FromSqlRaw(lotQuery, now)
                .FirstOrDefaultAsync(ct);

            if (lot == null)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            string outcome;
            if (lot.CurrentBidderId.HasValue)
            {
                var bids = await context.Bids
                    .Where(b => b.LotId == lot.Id && b.BidderId == lot.CurrentBidderId)
                    .ToListAsync(ct);

                var winningBid = bids
                    .OrderByDescending(b => b.Amount)
                    .ThenBy(b => b.Timestamp)
                    .FirstOrDefault();

                if (winningBid != null)
                {
                    lot.Close(now);
                    if (!lot.ReservePrice.HasValue || winningBid.Amount >= lot.ReservePrice.Value)
                    {
                        lot.MarkAsSold();
                        outcome = "Sold";
                    }
                    else
                    {
                        lot.SubmitToConditional();
                        outcome = "Conditional";
                    }

                    var deadline = businessDayCalculator.AddBusinessDays(now, _options.SettlementPaymentDeadlineBusinessDays);

                    var settlement = new Settlement(
                        Guid.NewGuid(),
                        lot.Id,
                        lot.CurrentBidderId.Value,
                        winningBid.Id,
                        lot.GetPriceBreakdown(),
                        now,
                        deadline,
                        SettlementStatus.PendingSignature
                    );
                    
                    context.Settlements.Add(settlement);

                    context.OutboxMessages.Add(new OutboxMessage
                    {
                        Type = "SignSettlement",
                        Content = settlement.Id.ToString(),
                        CreatedAt = now
                    });
                }
                else
                {
                    lot.Close(now);
                    lot.MarkAsUnsold();
                    outcome = "SoldFailedNoBidFound";
                }
            }
            else
            {
                lot.Close(now);
                lot.MarkAsUnsold();
                outcome = "Unsold";
            }

            var auditRecord = new AuditRecord(
                lot.CurrentBidderId ?? Guid.Empty,
                "LotClosed",
                now,
                $"Lot: {lot.Id}, Outcome: {outcome}, FinalPrice: {lot.CurrentPrice}, Reserve: {lot.ReservePrice}"
            );

            context.OutboxMessages.Add(new OutboxMessage
            {
                Type = "AuditRecord",
                Content = JsonSerializer.Serialize(auditRecord, AppJsonSerializerContext.Default.AuditRecord),
                CreatedAt = now
            });

            context.OutboxMessages.Add(new OutboxMessage
            {
                Type = "GenerateAuctionMinutes",
                Content = lot.Id.ToString(),
                CreatedAt = now
            });

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Error processing next expired lot.");
            throw;
        }
    }
}
