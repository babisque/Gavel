using Gavel.API.Contracts;
using Gavel.API.Controllers;
using Gavel.Application.Handlers.Bid.PlaceBid;
using MediatR;
using Microsoft.AspNetCore.Http;
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
            BidderId = Guid.NewGuid()
        };

        _mediator
            .Setup(m => m.Send(It.IsAny<PlaceBidCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
                
        // Act
        var result = await _bidController.PlaceBid(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        // You can also add this to check the response
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.Equal("Bid placed successfully.", apiResponse.Data);
        
        _mediator.Verify(m => m.Send(It.Is<PlaceBidCommand>(cmd =>
            cmd.AuctionItemId == request.AuctionItemId &&
            cmd.Amount == request.Amount &&
            cmd.BidderId == request.BidderId), It.IsAny<CancellationToken>()), Times.Once);
    }
}