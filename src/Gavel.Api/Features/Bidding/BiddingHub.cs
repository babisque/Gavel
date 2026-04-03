using Gavel.Core.Domain.Lots;
using Gavel.Api.Features.Bidding.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gavel.Api.Features.Bidding;

public interface IBiddingClient
{
    Task ReceiveBidUpdate(Guid lotId, PriceBreakdown breakdown);
    Task ReceiveTimeExtension(Guid lotId, DateTimeOffset newEndTime);
    Task ReceiveError(string errorCode, string message);
}

public class BiddingHub(IBiddingService biddingService) : Hub<IBiddingClient>
{
    public async Task JoinLotGroup(Guid lotId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lotId.ToString());
        
        var lot = await biddingService.GetLotForBiddingAsync(lotId);
        if (lot != null)
        {
            await Clients.Caller.ReceiveBidUpdate(lotId, lot.GetPriceBreakdown());
            if (lot.EndTime.HasValue)
            {
                await Clients.Caller.ReceiveTimeExtension(lotId, lot.EndTime.Value);
            }
        }
    }

    public async Task LeaveLotGroup(Guid lotId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lotId.ToString());
    }

    private Guid GetBidderId()
    {
        var idStr = Context.UserIdentifier ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var bidderId))
        {
            throw new UnauthorizedAccessException("Bidder identity could not be verified.");
        }
        
        return bidderId;
    }

    public async Task PlaceBid(Guid lotId, decimal amount)
    {
        try
        {
            var bidderId = GetBidderId();
            var sourceIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
            
            var (breakdown, newEndTime) = await biddingService.PlaceBidAsync(lotId, bidderId, amount, sourceIp);
            
            await Clients.Group(lotId.ToString()).ReceiveBidUpdate(lotId, breakdown);
            
            if (newEndTime.HasValue)
            {
                await Clients.Group(lotId.ToString()).ReceiveTimeExtension(lotId, newEndTime.Value);
            }
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.ReceiveError("BID_RULE_VIOLATION", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await Clients.Caller.ReceiveError("INVALID_BID_AMOUNT", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Clients.Caller.ReceiveError("UNAUTHORIZED", ex.Message);
        }
        catch (Exception)
        {
            await Clients.Caller.ReceiveError("INTERNAL_ERROR", "An unexpected error occurred while placing your bid.");
        }
    }
    
    public async Task PlaceProxyBid(Guid lotId, decimal maxAmount)
    {
        try
        {
            var bidderId = GetBidderId();
            var sourceIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
            
            var (breakdown, newEndTime) = await biddingService.PlaceProxyBidAsync(lotId, bidderId, maxAmount, sourceIp);
            
            await Clients.Group(lotId.ToString()).ReceiveBidUpdate(lotId, breakdown);
            
            if (newEndTime.HasValue)
            {
                await Clients.Group(lotId.ToString()).ReceiveTimeExtension(lotId, newEndTime.Value);
            }
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.ReceiveError("PROXY_RULE_VIOLATION", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await Clients.Caller.ReceiveError("INVALID_PROXY_AMOUNT", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Clients.Caller.ReceiveError("UNAUTHORIZED", ex.Message);
        }
        catch (Exception)
        {
            await Clients.Caller.ReceiveError("INTERNAL_ERROR", "An unexpected error occurred while placing your proxy bid.");
        }
    }
}
