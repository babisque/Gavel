using Gavel.API.Hubs;
using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Gavel.API.Services;

public class SignalRBidNotificationService(IHubContext<BidHub> hubContext) : IBidNotificationService
{
    public async Task NotifyNewBidAsync(Bid newBid)
    {
        await hubContext.Clients
            .Group(newBid.AuctionItemId.ToString())
            .SendAsync("NewBidPlaced", newBid);
    }

    public async Task NotifyAuctionClosedAsync(Guid auctionItemId)
    {
        await hubContext.Clients
            .Group(auctionItemId.ToString())
            .SendAsync("AuctionClosed", auctionItemId);
    }
}