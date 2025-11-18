using Gavel.API.Hubs;
using Gavel.Application.Interfaces;
using Gavel.Domain.Entities;
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
}