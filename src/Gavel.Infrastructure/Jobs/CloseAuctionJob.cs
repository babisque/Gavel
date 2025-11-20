using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gavel.Infrastructure.Jobs;

public class CloseAuctionJob(IUnitOfWork unitOfWork,
    IBidNotificationService bidNotificationService,
    ILogger<CloseAuctionJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var rawId = context.JobDetail.JobDataMap.GetString("AuctionItemId");
        if (!Guid.TryParse(rawId, out var auctionItemId)) return;

        var item = await unitOfWork.AuctionItems.GetByIdAsync(auctionItemId);
        
        if (item is null || item.Status != AuctionStatus.Active) return;
        
        logger.LogInformation($"AuctionItem {auctionItemId} has been closed.");

        item.Status = AuctionStatus.Finished;
        await unitOfWork.AuctionItems.UpdateAsync(item);
        await unitOfWork.CompleteAsync();
        
        await bidNotificationService.NotifyAuctionClosedAsync(auctionItemId);
    }
}