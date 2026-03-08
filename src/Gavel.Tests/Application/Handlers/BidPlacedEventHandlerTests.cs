using Gavel.Application.Handlers.Bids;
using Gavel.Domain.Entities;
using Gavel.Domain.Events;
using Gavel.Domain.Interfaces.Services;
using Gavel.Domain.ValueObjects;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class BidPlacedEventHandlerTests
{
    private readonly Mock<IBidNotificationService> _mockBidNotificationService;
    private readonly BidPlacedEventHandler _handler;

    public BidPlacedEventHandlerTests()
    {
        _mockBidNotificationService = new Mock<IBidNotificationService>();
        _handler = new BidPlacedEventHandler(_mockBidNotificationService.Object);
    }

    [Fact]
    public async Task Handle_WhenEventReceived_CallsNotificationService()
    {
        // Arrange
        var bid = new Bid(new Money(150m), Guid.NewGuid(), Guid.NewGuid());

        var notification = new BidPlacedEvent(bid);

        _mockBidNotificationService
            .Setup(x => x.NotifyNewBidAsync(It.IsAny<Bid>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockBidNotificationService.Verify(
            x => x.NotifyNewBidAsync(It.Is<Bid>(b => 
                b.Id == bid.Id && 
                b.Amount.Amount == bid.Amount.Amount && 
                b.BidderId == bid.BidderId)),
            Times.Once
        );
    }
}

