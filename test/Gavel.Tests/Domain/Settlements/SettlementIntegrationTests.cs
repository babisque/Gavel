using Gavel.Api.Features.Settlements.Services;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Settlements;

public class SettlementIntegrationTests : IDisposable
{
    private readonly GavelDbContext _context;
    private readonly ISettlementService _service;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SettlementService> _logger;
    private readonly IOptions<LotClosingOptions> _options;

    public SettlementIntegrationTests()
    {
        // For local simulation, we can use Sqlite. 
        // Note: Real 'SKIP LOCKED' requires PostgreSQL.
        var dbOptions = new DbContextOptionsBuilder<GavelDbContext>()
            .UseSqlite($"Data Source={Guid.NewGuid()}.db")
            .Options;

        _context = new GavelDbContext(dbOptions);
        _context.Database.EnsureCreated();
        
        _timeProvider = Substitute.For<TimeProvider>();
        _logger = Substitute.For<ILogger<SettlementService>>();
        
        var closingOptions = new LotClosingOptions();
        _options = Options.Create(closingOptions);
        
        _service = new SettlementService(_context, _timeProvider, _options, _logger);
    }

    [Test]
    public async Task SettlementEngine_ProcessesExpiredLots_AndCreatesSettlement()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        _timeProvider.GetUtcNow().Returns(now);
        
        var lot = new Lot(Guid.NewGuid(), Guid.NewGuid(), "Test Lot", 1000m);
        lot.AddPhoto("http://example.com/photo.jpg");
        lot.AttachPublicNotice("http://example.com/notice.pdf", "v1", now.AddDays(-3));
        lot.Schedule(now.AddDays(-2), now.AddDays(-1));
        var bidderId = Guid.NewGuid();
        lot.OpenForBidding(now.AddDays(-2));
        lot.PlaceBid(bidderId, 1500m, now.AddMinutes(-5));
        
        var bid = new Gavel.Core.Domain.Bidding.Bid(Guid.NewGuid(), lot.Id, bidderId, 1500m, now.AddMinutes(-5), "127.0.0.1");
        _context.Bids.Add(bid);
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();
        
        // Act
        // We override the internal method for simulation if we can't use real Postgres SKIP LOCKED
        await _service.ProcessExpiredLotsAsync(CancellationToken.None);
        
        // Assert
        var updatedLot = await _context.Lots.FindAsync(lot.Id);
        await That(updatedLot!.State).IsEqualTo(LotState.Sold);
        
        var settlement = await _context.Settlements.FirstOrDefaultAsync(s => s.LotId == lot.Id);
        await That(settlement).IsNotNull();
        await That(settlement!.PriceBreakdown.BidAmount).IsEqualTo(1500m);
        await That(settlement.Status).IsEqualTo(SettlementStatus.PendingSignature);
    }

    [Test]
    [Skip("This test requires a real PostgreSQL database to verify 'SKIP LOCKED' behavior.")]
    public async Task SettlementEngine_ConcurrentProcessing_OnlyOneSucceeds()
    {
        // This test would ideally start two parallel tasks calling ProcessNextExpiredLotAsync
        // on two different DbContext instances against a real Postgres DB.
        
        // With Sqlite/InMemory, we can't truly test SKIP LOCKED.
        // Instead, we verify the transactional integrity of the MarkAsSold operation.
        
        var now = DateTimeOffset.UtcNow;
        _timeProvider.GetUtcNow().Returns(now);
        
        var lot = new Lot(Guid.NewGuid(), Guid.NewGuid(), "Test Lot", 1000m);
        lot.AddPhoto("http://example.com/photo.jpg");
        lot.AttachPublicNotice("http://example.com/notice.pdf", "v1", now.AddDays(-3));
        lot.Schedule(now.AddDays(-2), now.AddDays(-1));
        var bidderId = Guid.NewGuid();
        lot.OpenForBidding(now.AddDays(-2));
        lot.PlaceBid(bidderId, 1500m, now.AddMinutes(-5));
        
        var bid = new Gavel.Core.Domain.Bidding.Bid(Guid.NewGuid(), lot.Id, bidderId, 1500m, now.AddMinutes(-5), "127.0.0.1");
        _context.Bids.Add(bid);
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();

        // Simulate two instances of the service
        var service1 = _service;
        var service2 = new SettlementService(_context, _timeProvider, _options, _logger);

        // This is a naive attempt to simulate concurrency on the same context (not ideal)
        // In a real scenario, they would have separate connections/contexts.
        
        var task1 = ((SettlementService)service1).ProcessNextExpiredLotAsync(now, CancellationToken.None);
        var task2 = ((SettlementService)service2).ProcessNextExpiredLotAsync(now, CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);
        
        // Verify that only one returned true (processed the lot)
        // Note: Without real SKIP LOCKED, this might fail or behave non-deterministically here.
        await That(results.Count(r => r)).IsEqualTo(1);
    }

    [Test]
    public async Task SettlementEngine_HandlesReservePrice_TransitionsToConditional()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        _timeProvider.GetUtcNow().Returns(now);
        
        var lot = new Lot(Guid.NewGuid(), Guid.NewGuid(), "Test Lot", 1000m);
        lot.AddPhoto("http://example.com/photo.jpg");
        lot.AttachPublicNotice("http://example.com/notice.pdf", "v1", now.AddDays(-3));
        lot.SetReservePrice(2000m); // Reserve Price > Current Bid
        lot.Schedule(now.AddDays(-2), now.AddDays(-1));
        var bidderId = Guid.NewGuid();
        lot.OpenForBidding(now.AddDays(-2));
        lot.PlaceBid(bidderId, 1500m, now.AddMinutes(-5));
        
        var bid = new Gavel.Core.Domain.Bidding.Bid(Guid.NewGuid(), lot.Id, bidderId, 1500m, now.AddMinutes(-5), "127.0.0.1");
        _context.Bids.Add(bid);
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();
        
        // Act
        await _service.ProcessExpiredLotsAsync(CancellationToken.None);
        
        // Assert
        var updatedLot = await _context.Lots.FindAsync(lot.Id);
        await That(updatedLot!.State).IsEqualTo(LotState.Conditional);
        
        var settlement = await _context.Settlements.FirstOrDefaultAsync(s => s.LotId == lot.Id);
        await That(settlement).IsNotNull(); // Settlement is still created for conditional sales
        await That(settlement!.Status).IsEqualTo(SettlementStatus.PendingSignature);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
