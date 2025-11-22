using System.Text.Json;
using Gavel.Domain.Constants;
using Gavel.Domain.Entities;
using Gavel.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gavel.Infrastructure.BackgroundServices;

public class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ISchedulerFactory schedulerFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == OutboxMessageTypes.ScheduleAuctionClose)
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(message.Payload);
                    var auctionId = data.GetProperty("auctionId").GetGuid();
                    var endTime = data.GetProperty("endTime").GetDateTime();
                    
                    var job = JobBuilder.Create<CloseAuctionJob>()
                        .WithIdentity($"CloseAuctionJob-{auctionId}", "AuctionClosers")
                        .UsingJobData("AuctionItemId", auctionId.ToString())
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"Trigger-{auctionId}", "AuctionClosers")
                        .StartAt(endTime)
                        .Build();
                    
                    if (!await scheduler.CheckExists(job.Key, stoppingToken))
                        await scheduler.ScheduleJob(job, trigger, stoppingToken);
                }

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= 3)
                {
                    message.ProcessedAt = DateTime.UtcNow;
                    logger.LogCritical(ex,
                        "Message {MessageId} failed after 3 retries and will be marked as processed.", message.Id);
                }
                else
                {
                    logger.LogWarning(ex, "Message {MessageId} processing failed. Retry count: {RetryCount}",
                        message.Id, message.RetryCount);
                }
            }
        }
        
        if (messages.Count != 0)
            await dbContext.SaveChangesAsync(stoppingToken);
    }
}