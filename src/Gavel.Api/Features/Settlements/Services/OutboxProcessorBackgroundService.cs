using Gavel.Api.Infrastructure.Data;
using Gavel.Api.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gavel.Api.Features.Settlements.Services;

/// <summary>
/// Refactored Outbox Processor that acts as a Dispatcher.
/// Resolves handlers for each message type and processes them concurrently.
/// Ensures resilience through individual task persistence and safe error handling.
/// </summary>
public class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<LotClosingOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{
    private readonly LotClosingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Dispatcher is starting with interval {Interval}.", _options.OutboxCheckInterval);

        using PeriodicTimer timer = new(_options.OutboxCheckInterval, timeProvider);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while dispatching outbox messages.");
            }
        }

        logger.LogInformation("Outbox Dispatcher is stopping.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        List<OutboxMessage> messages;
        var now = timeProvider.GetUtcNow();

        // 1. Claim Batch (Thread-safe claim using Row Versioning or DB Locks)
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
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
        }

        // 2. Parallel Dispatch with Resilient Updates
        await Parallel.ForEachAsync(messages, new ParallelOptions 
        { 
            MaxDegreeOfParallelism = _options.MaxOutboxParallelism,
            CancellationToken = ct 
        }, async (message, token) =>
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            var handlers = scope.ServiceProvider.GetServices<IOutboxHandler>();
            
            try
            {
                var handler = handlers.FirstOrDefault(h => h.CanHandle(message.Type));
                
                if (handler != null)
                {
                    await handler.HandleAsync(message, context, token);
                    
                    message.Status = OutboxMessageStatus.Completed;
                    message.ProcessedAt = timeProvider.GetUtcNow();
                    message.ErrorMessage = null;
                }
                else
                {
                    logger.LogWarning("No handler found for outbox message type: {Type}", message.Type);
                    message.Status = OutboxMessageStatus.Completed; // Skip to avoid infinite retry loop
                }

                // Resilient Persistence: Save success within the task scope
                context.OutboxMessages.Attach(message);
                context.Entry(message).State = EntityState.Modified;
                await context.SaveChangesAsync(token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process outbox message {Id} ({Type}). Attempting to record failure state.", message.Id, message.Type);
                
                try
                {
                    // Update state for retry
                    message.RetryCount++;
                    message.ErrorMessage = ex.Message;
                    message.Status = OutboxMessageStatus.Failed;
                    message.StartedProcessingAt = null; 

                    // Safe Save: Record error without crashing the worker
                    context.OutboxMessages.Attach(message);
                    context.Entry(message).State = EntityState.Modified;
                    await context.SaveChangesAsync(CancellationToken.None); // Use None to ensure failure state is recorded even if batch is cancelling
                }
                catch (Exception saveEx)
                {
                    logger.LogCritical(saveEx, "CRITICAL: Could not persist failure state for Outbox message {Id}.", message.Id);
                }
            }
        });
    }
}
