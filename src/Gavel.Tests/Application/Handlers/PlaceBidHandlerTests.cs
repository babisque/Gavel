using AutoMapper;
using Gavel.Application.Handlers.Bids.PlaceBid;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces.Services;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class PlaceBidHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IBidNotificationService> _mockBidNotificationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PlaceBidHandler _handler;

    public PlaceBidHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var mockPublisher = new Mock<IPublisher>();
        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();
        
        _context = new ApplicationDbContext(options, mockPublisher.Object, mockLogger.Object);

        _mockBidNotificationService = new Mock<IBidNotificationService>();
        _mockMapper = new Mock<IMapper>();

        _handler = new PlaceBidHandler(
            _context,
            _mockBidNotificationService.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Handler_ShouldThrowNotFoundException_WhenAuctionItemDoesNotExist()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid(), BidderId = Guid.NewGuid(), Amount = 100 };
        
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldThrowConflictException_WhenAuctionIsNotActive()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = bidderId, Amount = 150 };
        
        var auctionItem = new AuctionItem 
        { 
            Id = auctionItemId,
            Name = "Test Item",
            CurrentPrice = 100,
            Status = AuctionStatus.Finished,
            StartTime = DateTime.UtcNow.AddDays(-2),
            EndTime = DateTime.UtcNow.AddDays(-1),
            RowVersion = new byte[8]
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        var bid = new Bid { Id = Guid.NewGuid(), AuctionItemId = auctionItemId, BidderId = bidderId, Amount = command.Amount };
        _mockMapper.Setup(m => m.Map<Bid>(command)).Returns(bid);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldThrowConflictException_WhenBidIsLowerThanMinimum()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = bidderId, Amount = 104 }; // Less than 5% above 100
        
        var auctionItem = new AuctionItem 
        { 
            Id = auctionItemId,
            Name = "Test Item",
            CurrentPrice = 100, 
            Status = AuctionStatus.Active,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            RowVersion = new byte[8]
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        var bid = new Bid { Id = Guid.NewGuid(), AuctionItemId = auctionItemId, BidderId = bidderId, Amount = command.Amount };
        _mockMapper.Setup(m => m.Map<Bid>(command)).Returns(bid);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}