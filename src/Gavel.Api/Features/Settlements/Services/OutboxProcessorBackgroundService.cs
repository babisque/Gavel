using System.Text.Json;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Infrastructure.Logging;
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

        var messagesQuery = context.Database.IsNpgsql()
            ? @"SELECT * FROM ""OutboxMessages"" WHERE ""ProcessedAt"" IS NULL AND ""RetryCount"" < 5 FOR UPDATE SKIP LOCKED LIMIT {0}"
            : @"SELECT * FROM ""OutboxMessages"" WHERE ""ProcessedAt"" IS NULL AND ""RetryCount"" < 5 LIMIT {0}";

        var messages = await context.OutboxMessages
            .FromSqlRaw(messagesQuery, _options.OutboxBatchSize)
            .AsNoTracking()
            .ToListAsync(ct);

        if (messages.Count == 0) return;

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

                message.ProcessedAt = timeProvider.GetUtcNow();
                message.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                logger.LogWarning(ex, "Failed to process outbox message {Id}. Retry count: {Count}", message.Id, message.RetryCount);
            }
            
            context.OutboxMessages.Update(message);
        }

        await context.SaveChangesAsync(ct);
    }
}
