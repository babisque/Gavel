using System.Text.Json;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gavel.Api.Features.Settlements.Services;

public class OutboxProcessorBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<LotClosingOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{
    private readonly LotClosingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor Background Service is starting with interval {Interval}.", _options.OutboxCheckInterval);

        using PeriodicTimer timer = new(_options.OutboxCheckInterval, timeProvider);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing outbox messages.");
            }
        }

        logger.LogInformation("Outbox Processor Background Service is stopping.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        var signatureService = scope.ServiceProvider.GetRequiredService<IDigitalSignatureService>();

        List<OutboxMessage> messages;
        var now = timeProvider.GetUtcNow();

        await using (var transaction = await context.Database.BeginTransactionAsync(ct))
        {
            var timeoutLimit = now.AddMinutes(-5);

            var messagesQuery = context.Database.IsNpgsql()
                ? @"SELECT * FROM ""OutboxMessages"" 
                    WHERE ""Status"" IN ('Pending', 'Failed') 
                       OR (""Status"" = 'Processing' AND ""StartedProcessingAt"" < {0})
                    FOR UPDATE SKIP LOCKED 
                    LIMIT {1}"
                : @"SELECT * FROM ""OutboxMessages"" 
                    WHERE ""Status"" IN ('Pending', 'Failed') 
                       OR (""Status"" = 'Processing' AND ""StartedProcessingAt"" < {0})
                    LIMIT {1}";

            messages = await context.OutboxMessages
                .FromSqlRaw(messagesQuery, timeoutLimit, _options.OutboxBatchSize)
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return;
            }

            foreach (var message in messages)
            {
                message.Status = OutboxMessageStatus.Processing;
                message.StartedProcessingAt = now;
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == "AuditRecord")
                {
                    var record = JsonSerializer.Deserialize(message.Content, AppJsonSerializerContext.Default.AuditRecord);
                    if (record != null)
                    {
                        await auditLogger.LogAsync(record);
                    }
                }
                else if (message.Type == "SignSettlement")
                {
                    if (Guid.TryParse(message.Content, out var settlementId))
                    {
                        var settlement = await context.Settlements
                            .FirstOrDefaultAsync(s => s.Id == settlementId, ct);
                            
                        if (settlement != null)
                        {
                            var signature = await signatureService.SignSettlementAsync(settlement);
                            settlement.ApplySignature(signature);

                            await auditLogger.LogAsync(new AuditRecord(
                                settlement.BidderId,
                                "SettlementSigned",
                                timeProvider.GetUtcNow(),
                                $"Settlement: {settlement.Id}, Signature: {signature}"
                            ));
                        }
                    }
                }

                message.Status = OutboxMessageStatus.Completed;
                message.ProcessedAt = timeProvider.GetUtcNow();
                message.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                message.Status = OutboxMessageStatus.Failed;
                message.StartedProcessingAt = null; 
                logger.LogWarning(ex, "Failed to process outbox message {Id}. Retry count: {Count}", message.Id, message.RetryCount);
            }
            
            if (context.Entry(message).State == EntityState.Detached)
            {
                context.OutboxMessages.Attach(message);
            }
            context.Entry(message).State = EntityState.Modified;
        }

        await context.SaveChangesAsync(ct);
    }
}
