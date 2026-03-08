using Gavel.Application.Handlers.Bids.PlaceBid;
using Gavel.Domain.Entities;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Domain.ValueObjects;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class PlaceBidHandlerTests
{
    private readonly Mock<IAuctionItemRepository> _mockAuctionItemRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly PlaceBidHandler _handler;

    public PlaceBidHandlerTests()
    {
        _mockAuctionItemRepository = new Mock<IAuctionItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new PlaceBidHandler(
            _mockAuctionItemRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handler_ShouldThrowNotFoundException_WhenAuctionItemDoesNotExist()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid(), BidderId = Guid.NewGuid(), Amount = 100m };
        _mockAuctionItemRepository
            .Setup(r => r.GetByIdAsync(command.AuctionItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuctionItem?)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handler_ShouldThrowConflictException_WhenAuctionIsNotActive()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = bidderId, Amount = 150m };
        
        // Past end-time guarantees a conflict when trying to place a bid.
        var auctionItem = new AuctionItem("Test Item", "Test Description", new Money(100m), DateTime.UtcNow.AddMinutes(-1));
        _mockAuctionItemRepository
            .Setup(r => r.GetByIdAsync(auctionItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auctionItem);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handler_ShouldThrowConflictException_WhenBidIsLowerThanMinimum()
    {
        // Arrange
        var auctionItemId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var command = new PlaceBidCommand { AuctionItemId = auctionItemId, BidderId = bidderId, Amount = 104m }; // Less than 5% above 100
        
        var auctionItem = new AuctionItem("Test Item", "Test Description", new Money(100m), DateTime.UtcNow.AddDays(1));
        _mockAuctionItemRepository
            .Setup(r => r.GetByIdAsync(auctionItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auctionItem);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}