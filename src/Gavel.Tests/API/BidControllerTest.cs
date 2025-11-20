using Gavel.API.Controllers;
using Gavel.Application.Handlers.Bids.PlaceBid;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gavel.Tests.API;

public class BidControllerTest
{
    private readonly Mock<IMediator> _mediator;
    private readonly BidController _bidController;

    public BidControllerTest()
    {
        _mediator = new Mock<IMediator>();
        _bidController = new BidController(_mediator.Object);
    }
    
    [Fact]
    public async Task PlaceBid_WhenCalled_ReturnsOkResult()
    {
        // Arrange
        var request = new PlaceBidCommand
        {
            AuctionItemId = Guid.NewGuid(),
            Amount = 100.0m,
            BidderName = "test name"
        };

        _mediator
            .Setup(m => m.Send(It.IsAny<PlaceBidCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
                
        // Act
        var result = await _bidController.PlaceBid(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var val = okResult.Value;
        var messageProperty = val.GetType().GetProperty("message");
        var messageValue = messageProperty.GetValue(val, null) as string;

        Assert.Equal("Bid placed successfully.", messageValue);
        
        _mediator.Verify(m => m.Send(It.Is<PlaceBidCommand>(cmd =>
            cmd.AuctionItemId == request.AuctionItemId &&
            cmd.Amount == request.Amount &&
            cmd.BidderName == request.BidderName), It.IsAny<CancellationToken>()), Times.Once);
    }
}