namespace Gavel.Tests.Domain.Auctions;

using Gavel.Api.Features.Auctions.Services;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Lots;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Assertions.Extensions;
using static TUnit.Assertions.Assert;

public class LotManagementServiceTests : IDisposable
{
    private readonly GavelDbContext _context;
    private readonly LotManagementService _service;
    private readonly TimeProvider _timeProvider;

    public LotManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<GavelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new GavelDbContext(options);
        
        _timeProvider = Substitute.For<TimeProvider>();
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        
        _service = new LotManagementService(_context, _timeProvider);
    }

    [Test]
    public async Task CreateLot_Succeeds()
    {
        var auctionId = Guid.NewGuid();
        var lotId = await _service.CreateLotAsync(auctionId, "Test Lot", 1000m);
        
        var lot = await _context.Lots.FindAsync(lotId);
        await That(lot).IsNotNull();
        await That(lot!.Title).IsEqualTo("Test Lot");
    }

    [Test]
    public async Task ScheduleLotAsync_WhenMandatoryAssetsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var lotId = await _service.CreateLotAsync(auctionId, "Incomplete Lot", 1000m);

        // Act & Assert
        var action = () => _service.ScheduleLotAsync(lotId, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2));
        await That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetLot_ReturnsLot_WhenExists()
    {
        // Arrange
        var lotId = Guid.NewGuid();
        var lot = new Lot(lotId, Guid.NewGuid(), "Lot", 500m);
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();

        // Act
        var loadedLot = await _service.GetLotAsync(lotId);

        // Assert
        await That(loadedLot).IsNotNull();
        await That(loadedLot!.Id).IsEqualTo(lotId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
