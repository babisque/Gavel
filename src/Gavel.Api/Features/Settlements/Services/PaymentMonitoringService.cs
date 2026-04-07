using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Settlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gavel.Api.Features.Settlements.Services;

/// <summary>
/// Background service that monitors settlements for legal payment deadlines.
/// If a settlement is past its deadline, it marks it as Overdue and 
/// queues an outbox message to block the delinquent bidder.
/// </summary>
public class PaymentMonitoringService(
    IServiceProvider serviceProvider,
    IOptions<LotClosingOptions> options,
    TimeProvider timeProvider,
    ILogger<PaymentMonitoringService> logger) : BackgroundService
{
    private readonly LotClosingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Payment Monitoring Service is starting.");

        using PeriodicTimer timer = new(_options.CheckInterval, timeProvider);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await MonitorPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while monitoring payments.");
            }
        }

        logger.LogInformation("Payment Monitoring Service is stopping.");
    }

    private async Task MonitorPaymentsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
        
        var now = timeProvider.GetUtcNow();

        // Fetch settlements that are Signed or PendingSignature but past their legal PaymentDeadline
        // Order by deadline to prioritize the oldest legal violations first.
        var overdueSettlements = await context.Settlements
            .Where(s => (s.Status == SettlementStatus.PendingSignature || s.Status == SettlementStatus.Signed) 
                        && s.PaymentDeadline < now)
            .OrderBy(s => s.PaymentDeadline)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (overdueSettlements.Count == 0) return;

        logger.LogInformation("Found {Count} overdue settlements. Queuing sanctions.", overdueSettlements.Count);

        foreach (var settlement in overdueSettlements)
        {
            try
            {
                // 1. Transition to Overdue (Domain Logic)
                settlement.MarkAsOverdue();
                
                // 2. Queue Outbox message for Bidder Blocking (Transactional Integrity)
                context.OutboxMessages.Add(new OutboxMessage
                {
                    Type = "BlockDelinquentBidder",
                    Content = settlement.BidderId.ToString(),
                    CreatedAt = now
                });

                logger.LogWarning("Settlement {SettlementId} is Overdue. Blocking request queued for Bidder {BidderId}.", settlement.Id, settlement.BidderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process overdue settlement {SettlementId}.", settlement.Id);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
