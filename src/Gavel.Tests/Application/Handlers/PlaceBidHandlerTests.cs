using AutoMapper;
using Gavel.Application.Handlers.Bids.PlaceBid;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces.Services;
using Gavel.Infrastructure;
using Microsoft.EntityFrameworkCore;
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
            .Options;

        _context = new ApplicationDbContext(options);

        _mockBidNotificationService = new Mock<IBidNotificationService>();
        _mockMapper = new Mock<IMapper>();

        _handler = new PlaceBidHandler(
            _context,
            _mockBidNotificationService.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Handler_ShouldPlaceBid_WhenRequestIsValid()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        
        var command = new PlaceBidCommand 
        { 
            AuctionItemId = auctionItemId, 
            BidderId = bidderId,
            Amount = 150 
        };

        var auctionItem = new AuctionItem 
        { 
            Id = auctionItemId, 
            CurrentPrice = 100, 
            Status = AuctionStatus.Active,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        var bid = new Bid { Id = Guid.NewGuid(), AuctionItemId = auctionItemId, BidderId = bidderId, Amount = command.Amount };
        _mockMapper.Setup(m => m.Map<Bid>(command)).Returns(bid);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedItem = await _context.AuctionItems.FindAsync(auctionItemId);
        var savedBid = await _context.Bids.FirstOrDefaultAsync(b => b.AuctionItemId == auctionItemId);

        Assert.NotNull(updatedItem);
        Assert.Equal(150, updatedItem.CurrentPrice);
        
        Assert.NotNull(savedBid);
        Assert.Equal(150, savedBid.Amount);

        // 2. Verify Notification Service was called
        _mockBidNotificationService.Verify(s => s.NotifyNewBidAsync(It.Is<Bid>(b => b.Amount == 150)), Times.Once);
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
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = Guid.NewGuid(), Amount = 150 };
        
        var auctionItem = new AuctionItem 
        { 
            Id = auctionItemId, 
            Status = AuctionStatus.Finished,
            EndTime = DateTime.UtcNow.AddDays(1) 
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldThrowConflictException_WhenBidIsLowerThanCurrentPrice()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = Guid.NewGuid(), Amount = 50 };
        
        var auctionItem = new AuctionItem 
        { 
            Id = auctionItemId, 
            CurrentPrice = 100, 
            Status = AuctionStatus.Active,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}