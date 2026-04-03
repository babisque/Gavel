namespace Gavel.Tests.Domain.Bidding;

using Gavel.Core.Domain.Bidding;
using Gavel.Core.Domain.Lots;
using NSubstitute;
using TUnit.Core;
using static TUnit.Assertions.Assert;

public class BiddingEngineTests
{
    private static readonly Guid AuctionId = Guid.NewGuid();
    private static readonly Guid LotId = Guid.NewGuid();
    private static readonly Guid BidderA = Guid.NewGuid();
    private static readonly Guid BidderB = Guid.NewGuid();
    private static readonly Guid BidderC = Guid.NewGuid();

    private Lot CreateActiveLot(decimal startingPrice, decimal minimumIncrement = 500m)
    {
        var lot = new Lot(LotId, AuctionId, "Test Lot", startingPrice, minimumIncrement);
        lot.AddPhoto("p1.jpg");
        lot.AttachPublicNotice("url", "v1", DateTimeOffset.UtcNow);
        lot.Schedule(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        lot.OpenForBidding(DateTimeOffset.UtcNow);
        return lot;
    }

    [Test]
    public async Task ProcessManualBid_WithValidIncrement_Succeeds()
    {
        var lot = CreateActiveLot(5000m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();

        var result = engine.ProcessManualBid(lot, BidderA, 5000m, DateTimeOffset.UtcNow, proxies);

        await That(result.WinningBid.BidderId).IsEqualTo(BidderA);
        await That(result.WinningBid.Amount).IsEqualTo(5000m);
        await That(lot.CurrentPrice).IsEqualTo(5000m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderA);
    }

    [Test]
    public async Task ProcessManualBid_BelowIncrement_ThrowsInvalidOperationException()
    {
        var lot = CreateActiveLot(5000m, 500m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();

        // First bid
        engine.ProcessManualBid(lot, BidderA, 5000m, DateTimeOffset.UtcNow, proxies);

        // Second bid too low
        Action action = () => engine.ProcessManualBid(lot, BidderB, 5100m, DateTimeOffset.UtcNow, proxies);

        await That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ProxyBidding_OutbidsManualBid()
    {
        var lot = CreateActiveLot(5000m, 500m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();
        var now = DateTimeOffset.UtcNow;

        // User A sets proxy
        engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderA, 10000m, now), now, proxies);
        
        // Initial state: A is winning at 5000m
        await That(lot.CurrentPrice).IsEqualTo(5000m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderA);

        // User B bids 6000m manually
        var result = engine.ProcessManualBid(lot, BidderB, 6000m, now.AddSeconds(1), proxies);

        // User A should automatically outbid User B at 6500m
        await That(result.WinningBid.BidderId).IsEqualTo(BidderA);
        await That(result.WinningBid.Amount).IsEqualTo(6500m);
        await That(lot.CurrentPrice).IsEqualTo(6500m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderA);
    }

    [Test]
    public async Task ProxyBidding_MaximumTie_FirstProxyWins()
    {
        var lot = CreateActiveLot(5000m, 500m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();
        var now = DateTimeOffset.UtcNow;

        // User A sets proxy
        engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderA, 10000m, now), now, proxies);

        // User C sets exactly same max proxy
        var result = engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderC, 10000m, now.AddSeconds(1)), now.AddSeconds(1), proxies);

        // User A should remain the leader at 10000m
        await That(result.WinningBid.BidderId).IsEqualTo(BidderA);
        await That(result.WinningBid.Amount).IsEqualTo(10000m);
        await That(lot.CurrentPrice).IsEqualTo(10000m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderA);
    }

    [Test]
    public async Task SoftClose_ExtendsEndTime_WhenBidIsNearExpiry()
    {
        var lot = CreateActiveLot(5000m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();
        
        var originalEndTime = lot.EndTime!.Value;
        var now = originalEndTime.AddSeconds(-30); // 30 seconds before closing

        var result = engine.ProcessManualBid(lot, BidderA, 5000m, now, proxies);

        await That(lot.State).IsEqualTo(LotState.Closing);
        await That(lot.EndTime!.Value).IsGreaterThan(originalEndTime);
        await That(result.NewEndTime).IsEqualTo(lot.EndTime);
        await That(lot.EndTime).IsEqualTo(now.Add(lot.SoftCloseWindow));
    }

    [Test]
    public async Task ProxyBidding_LimitReached_StopsAutoBidding()
    {
        var lot = CreateActiveLot(5000m, 500m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();
        var now = DateTimeOffset.UtcNow;

        // User A sets proxy
        engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderA, 10000m, now), now, proxies);

        // User B submits manual bid higher than A's max
        var result = engine.ProcessManualBid(lot, BidderB, 11000m, now.AddSeconds(1), proxies);

        // User B wins
        await That(result.WinningBid.BidderId).IsEqualTo(BidderB);
        await That(result.WinningBid.Amount).IsEqualTo(11000m);
        await That(lot.CurrentPrice).IsEqualTo(11000m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderB);
    }

    [Test]
    public async Task ConcurrentProxyBids_ResolveCorrectly_WithTieBreaks()
    {
        var lot = CreateActiveLot(5000m, 500m);
        var engine = new BiddingEngine();
        var proxies = new List<ProxyBid>();
        var now = DateTimeOffset.UtcNow;

        // User A sets proxy to 8000
        engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderA, 8000m, now), now, proxies);

        // User B sets proxy to 10000 (Beats A)
        engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderB, 10000m, now.AddSeconds(1)), now.AddSeconds(1), proxies);

        // User C sets proxy to 10000 (Tie with B, but B was earlier)
        var result = engine.ProcessProxyBid(lot, new ProxyBid(Guid.NewGuid(), LotId, BidderC, 10000m, now.AddSeconds(2)), now.AddSeconds(2), proxies);

        // B should be winning at 10000 because B was earlier than C
        await That(result.WinningBid.BidderId).IsEqualTo(BidderB);
        await That(result.WinningBid.Amount).IsEqualTo(10000m);
        await That(lot.CurrentPrice).IsEqualTo(10000m);
        await That(lot.CurrentBidderId).IsEqualTo(BidderB);
    }
}
