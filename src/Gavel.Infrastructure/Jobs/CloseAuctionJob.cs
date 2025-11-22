using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gavel.Infrastructure.Jobs;

public class CloseAuctionJob(ApplicationDbContext context,
    IBidNotificationService bidNotificationService,
    ILogger<CloseAuctionJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext executionContext)
    {
        var rawId = executionContext.JobDetail.JobDataMap.GetString("AuctionItemId");
        if (!Guid.TryParse(rawId, out var auctionItemId)) return;

        var item = await context.AuctionItems.FindAsync(auctionItemId);
        
        if (item is null || item.Status != AuctionStatus.Active) return;
        
        logger.LogInformation($"AuctionItem {auctionItemId} has been closed.");

        item.Status = AuctionStatus.Finished;

        await context.SaveChangesAsync();
        
        await bidNotificationService.NotifyAuctionClosedAsync(auctionItemId);
    }
}