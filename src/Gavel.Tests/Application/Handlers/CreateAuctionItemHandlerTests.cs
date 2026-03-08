using Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class CreateAuctionItemHandlerTests
{
    private readonly Mock<IAuctionItemRepository> _mockAuctionItemRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateAuctionItemHandler _handler;

    public CreateAuctionItemHandlerTests()
    {
        _mockAuctionItemRepository = new Mock<IAuctionItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new CreateAuctionItemHandler(_mockAuctionItemRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_CreatesAuctionItemAndOutboxMessage()
    {
        // Arrange
        var command = new CreateAuctionItemCommand
        {
            Name = "Test Auction",
            Description = "Test Description",
            InitialPrice = 100m,
            EndTime = DateTime.UtcNow.AddDays(7)
        };
        Gavel.Domain.Entities.AuctionItem? createdItem = null;

        _mockAuctionItemRepository
            .Setup(r => r.AddAsync(It.IsAny<Gavel.Domain.Entities.AuctionItem>(), It.IsAny<CancellationToken>()))
            .Callback<Gavel.Domain.Entities.AuctionItem, CancellationToken>((item, _) => createdItem = item)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        Assert.NotNull(createdItem);
        Assert.Equal(result, createdItem!.Id);
        Assert.Equal(command.Name, createdItem.Name);
        Assert.Equal(command.Description, createdItem.Description);
        Assert.Equal(command.InitialPrice, createdItem.InitialPrice.Amount);
        Assert.Equal(command.InitialPrice, createdItem.CurrentPrice.Amount);
        Assert.Equal(AuctionStatus.Active, createdItem.Status);

        _mockAuctionItemRepository.Verify(
            r => r.AddAsync(It.IsAny<Gavel.Domain.Entities.AuctionItem>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalled_SetsStartTimeToNow()
    {
        // Arrange
        var command = new CreateAuctionItemCommand
        {
            Name = "Test Auction",
            Description = "Test Description",
            InitialPrice = 100m,
            EndTime = DateTime.UtcNow.AddDays(7)
        };
        Gavel.Domain.Entities.AuctionItem? createdItem = null;

        _mockAuctionItemRepository
            .Setup(r => r.AddAsync(It.IsAny<Gavel.Domain.Entities.AuctionItem>(), It.IsAny<CancellationToken>()))
            .Callback<Gavel.Domain.Entities.AuctionItem, CancellationToken>((item, _) => createdItem = item)
            .Returns(Task.CompletedTask);

        var beforeTime = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.NotNull(createdItem);
        Assert.True(createdItem!.StartTime >= beforeTime);
        Assert.True(createdItem.StartTime <= afterTime);
    }
}

