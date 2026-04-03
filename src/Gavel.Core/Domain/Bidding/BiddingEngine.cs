namespace Gavel.Core.Domain.Bidding;

using Gavel.Core.Domain.Lots;

public class BiddingEngine
{
    public BidResult ProcessManualBid(
        Lot lot, 
        Guid bidderId, 
        decimal manualAmount, 
        DateTimeOffset now, 
        List<ProxyBid> activeProxyBids,
        string? sourceIp = null)
    {
        decimal minRequired = lot.CurrentBidderId == null ? lot.StartingPrice : lot.CurrentPrice + lot.MinimumIncrement;

        if (manualAmount < minRequired)
        {
            throw new InvalidOperationException($"Bid must be at least {minRequired}.");
        }

        lot.PlaceBid(bidderId, manualAmount, now);
        
        return EvaluateProxyBids(lot, bidderId, manualAmount, now, activeProxyBids, sourceIp);
    }
    
    public BidResult ProcessProxyBid(
        Lot lot, 
        ProxyBid newProxy, 
        DateTimeOffset now, 
        List<ProxyBid> activeProxyBids,
        string? sourceIp = null)
    {
        decimal minRequired = lot.CurrentBidderId == null ? lot.StartingPrice : lot.CurrentPrice + lot.MinimumIncrement;
            
        if (newProxy.MaxAmount < minRequired)
            throw new InvalidOperationException($"Proxy bid maximum {newProxy.MaxAmount} is lower than the minimum required bid of {minRequired}.");

        activeProxyBids.Add(newProxy);
        
        if (lot.CurrentBidderId == null || newProxy.MaxAmount > lot.CurrentPrice)
        {
            if (lot.CurrentBidderId == null || newProxy.MaxAmount >= lot.CurrentPrice + lot.MinimumIncrement)
            {
                 lot.PlaceBid(newProxy.BidderId, minRequired, now);
            }
        }

        return EvaluateProxyBids(lot, lot.CurrentBidderId ?? newProxy.BidderId, lot.CurrentPrice, now, activeProxyBids, sourceIp);
    }

    private BidResult EvaluateProxyBids(
        Lot lot, 
        Guid currentLeaderId, 
        decimal currentPrice, 
        DateTimeOffset now, 
        List<ProxyBid> activeProxyBids,
        string? sourceIp = null)
    {
        var sortedProxies = activeProxyBids
            .OrderByDescending(p => p.MaxAmount)
            .ThenBy(p => p.CreatedAt)
            .ToList();

        bool changesMade = true;
        
        while (changesMade)
        {
            changesMade = false;
            
            var leaderProxy = sortedProxies.FirstOrDefault(p => p.BidderId == currentLeaderId);
            decimal leaderMax = leaderProxy?.MaxAmount ?? currentPrice;

            var challenger = sortedProxies.FirstOrDefault(p => p.BidderId != currentLeaderId && p.MaxAmount >= currentPrice);

            if (challenger == null) break;

            if (leaderProxy != null)
            {
                if (leaderMax > challenger.MaxAmount)
                {
                    decimal nextPrice = challenger.MaxAmount + lot.MinimumIncrement;
                    decimal newPrice = Math.Min(leaderMax, nextPrice);
                    
                    if (newPrice > currentPrice)
                    {
                        currentPrice = newPrice;
                        lot.PlaceBid(currentLeaderId, currentPrice, now);
                        changesMade = true;
                    }
                }
                else if (leaderMax == challenger.MaxAmount)
                {
                    if (leaderProxy.CreatedAt <= challenger.CreatedAt)
                    {
                        if (currentPrice < leaderMax)
                        {
                            currentPrice = leaderMax;
                            lot.PlaceBid(currentLeaderId, currentPrice, now);
                            changesMade = true;
                        }
                    }
                    else
                    {
                        currentPrice = challenger.MaxAmount;
                        currentLeaderId = challenger.BidderId;
                        lot.PlaceBid(currentLeaderId, currentPrice, now);
                        changesMade = true;
                    }
                }
                else
                {
                    decimal nextPrice = leaderMax + lot.MinimumIncrement;
                    decimal newPrice = Math.Min(challenger.MaxAmount, nextPrice);
                    
                    currentPrice = newPrice;
                    currentLeaderId = challenger.BidderId;
                    lot.PlaceBid(currentLeaderId, currentPrice, now);
                    changesMade = true;
                }
            }
            else
            {
                if (challenger.MaxAmount >= currentPrice + lot.MinimumIncrement)
                {
                    currentPrice = currentPrice + lot.MinimumIncrement;
                    currentLeaderId = challenger.BidderId;
                    lot.PlaceBid(currentLeaderId, currentPrice, now);
                    changesMade = true;
                }
                else if (challenger.MaxAmount > currentPrice)
                {
                    currentPrice = challenger.MaxAmount;
                    currentLeaderId = challenger.BidderId;
                    lot.PlaceBid(currentLeaderId, currentPrice, now);
                    changesMade = true;
                }
            }
        }

        return new BidResult(
            new Bid(Guid.NewGuid(), lot.Id, currentLeaderId, currentPrice, now, sourceIp ?? "system"),
            lot.EndTime
        );
    }
}

public record BidResult(Bid WinningBid, DateTimeOffset? NewEndTime);
