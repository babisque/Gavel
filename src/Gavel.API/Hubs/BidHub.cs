using Microsoft.AspNetCore.SignalR;

namespace Gavel.API.Hubs;

public class BidHub : Hub
{
    public async Task JoinAuctionRoom(Guid auctionItemId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, auctionItemId.ToString());
    }
    
    public async Task LeaveAuctionGroup(string auctionItemId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, auctionItemId);
    }
}