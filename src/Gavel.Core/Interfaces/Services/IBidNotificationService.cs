using Gavel.Domain.Entities;

namespace Gavel.Domain.Interfaces.Services;

public interface IBidNotificationService
{
    Task NotifyNewBidAsync(Bid newBid);
    Task NotifyAuctionClosedAsync(Guid auctionItemId);
}