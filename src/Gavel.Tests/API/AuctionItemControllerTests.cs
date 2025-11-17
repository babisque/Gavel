using Gavel.API.Contracts;
using Gavel.API.Controllers;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gavel.Tests.API;

public class AuctionItemControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly AuctionItemController _controller;

    public AuctionItemControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new AuctionItemController(_mockMediator.Object);
    }

    [Fact]
    public async Task GetAuctionItems_WhenCalled_ReturnsOkResult()
    {
        // Arrange
        var request = new GetAuctionItemsQuery { Page = 1, Size = 10 };
        var expectedItems = new List<GetAuctionItemsResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Item 1" },
            new() { Id = Guid.NewGuid(), Name = "Item 2" }
        };
        var expectedTotalCount = 2;
        var mediatorResponse = (Items: expectedItems, TotalCount: expectedTotalCount);
        
        _mockMediator
            .Setup(m => m.Send(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediatorResponse);
        
        // Act
        var result = await _controller.GetAuctionItems(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<GetAuctionItemsResponse>>>(okResult.Value);
        
        Assert.Equal(expectedItems, apiResponse.Data);
        Assert.NotNull(apiResponse.Meta);
        Assert.Equal(request.Page, apiResponse.Meta.Page);
        Assert.Equal(request.Size, apiResponse.Meta.PageSize);
        Assert.Equal(expectedTotalCount, apiResponse.Meta.TotalRecords);
        
        _mockMediator.Verify(m => m.Send(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuctionItemById_WhenCalled_ReturnsOkResult()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var expectedItem = new GetAuctionItemByIdResponse { Id = itemId, Name = "Item 1", Description = "Description 1" };

        _mockMediator
            .Setup(m => m.Send(It.Is<GetAuctionItemByIdQuery>(q => q.Id == itemId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItem);
        
        // Act
        var result = await _controller.GetAuctionItemById(itemId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<GetAuctionItemByIdResponse>>(okResult.Value);
        
        Assert.Equal(expectedItem, apiResponse.Data);
        
        _mockMediator.Verify(m => m.Send(It.Is<GetAuctionItemByIdQuery>(q => q.Id == itemId), It.IsAny<CancellationToken>()), Times.Once);
    }
}