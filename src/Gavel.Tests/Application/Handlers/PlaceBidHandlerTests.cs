using AutoMapper;
using Gavel.Application.Handlers.Bids.PlaceBid;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Domain.Interfaces.Services;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class PlaceBidHandlerTests
{
    private readonly Mock<IBidRepository> _mockBidRepository;
    private readonly Mock<IAuctionItemRepository> _mockAuctionItemRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBidNotificationService> _bidNotificationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PlaceBidHandler _bidHandler;

    public PlaceBidHandlerTests()
    {
        _mockBidRepository = new Mock<IBidRepository>();
        _mockAuctionItemRepository = new Mock<IAuctionItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _bidNotificationService = new Mock<IBidNotificationService>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(uow => uow.Bids).Returns(_mockBidRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.AuctionItems).Returns(_mockAuctionItemRepository.Object);

        _bidHandler = new PlaceBidHandler(
            _mockUnitOfWork.Object,
            _bidNotificationService.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Handler_ShouldPlaceBid_WhenRequestIsValid()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid(), Amount = 150 };
        var auctionItem = new AuctionItem { Id = command.AuctionItemId, CurrentPrice = 100, Status = AuctionStatus.Active };
        var bid = new Bid { Id = Guid.NewGuid(), AuctionItemId = command.AuctionItemId, Amount = command.Amount };

        _mockAuctionItemRepository.Setup(r => r.GetByIdAsync(command.AuctionItemId))
            .ReturnsAsync(auctionItem);
        _mockMapper.Setup(m => m.Map<Bid>(command)).Returns(bid);
        _mockBidRepository.Setup(r => r.CreateAsync(bid)).ReturnsAsync(bid);

        // Act
        await _bidHandler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(uow => uow.Bids.CreateAsync(bid), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.AuctionItems.UpdateAsync(auctionItem), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _bidNotificationService.Verify(s => s.NotifyNewBidAsync(bid), Times.Once);
        Assert.Equal(command.Amount, auctionItem.CurrentPrice);
    }

    [Fact]
    public async Task Handler_ShouldThrowNotFoundException_WhenAuctionItemDoesNotExist()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid() };
        _mockAuctionItemRepository.Setup(r => r.GetByIdAsync(command.AuctionItemId))
            .ReturnsAsync((AuctionItem)null!);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _bidHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldThrowApplicationException_WhenAuctionIsNotActive()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid() };
        var auctionItem = new AuctionItem { Id = command.AuctionItemId, Status = AuctionStatus.Finished };
        _mockAuctionItemRepository.Setup(r => r.GetByIdAsync(command.AuctionItemId))
            .ReturnsAsync(auctionItem);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _bidHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldThrowApplicationException_WhenBidIsLowerThanCurrentPrice()
    {
        // Arrange
        var command = new PlaceBidCommand { AuctionItemId = Guid.NewGuid(), Amount = 50 };
        var auctionItem = new AuctionItem { Id = command.AuctionItemId, CurrentPrice = 100, Status = AuctionStatus.Active };
        _mockAuctionItemRepository.Setup(r => r.GetByIdAsync(command.AuctionItemId))
            .ReturnsAsync(auctionItem);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _bidHandler.Handle(command, CancellationToken.None));
    }
}