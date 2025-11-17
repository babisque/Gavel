using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Repositories;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class GetAuctionItemsHandlerTests
{
    private readonly Mock<IAuctionItemRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetAuctionItemsHandler _handler;

    public GetAuctionItemsHandlerTests()
    {
        _mockRepository = new Mock<IAuctionItemRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetAuctionItemsHandler(_mockRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnMappedDataAndTotalCount()
    {
        // Arrange
        var request = new GetAuctionItemsQuery { Page = 1, Size = 10 };
        var cancellationToken = CancellationToken.None;

        var dbItems = new List<AuctionItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Item 1" },
            new() { Id = Guid.NewGuid(), Name = "Item 2" }
        };
        var expectedTotalCount = 2;
        var repositoryResponse = (Items: (IReadOnlyCollection<AuctionItem>)dbItems, TotalCount: expectedTotalCount);
        
        var mappedItems = new List<GetAuctionItemsResponse>
        {
            new() { Id = dbItems[0].Id, Name = "Item 1" },
            new() { Id = dbItems[1].Id, Name = "Item 2" }
        };

        _mockRepository
            .Setup(r => r.GetAllPagedAsync(request.Page, request.Size))
            .ReturnsAsync(repositoryResponse);

        _mockMapper
            .Setup(m => m.Map<List<GetAuctionItemsResponse>>(dbItems))
            .Returns(mappedItems);
        
        // Act
        var (resultItems, resultTotalCount) = await _handler.Handle(request, cancellationToken);
        
        // Assert
        Assert.Equal(mappedItems, resultItems);
        Assert.Equal(expectedTotalCount, resultTotalCount);
        
        _mockRepository.Verify(r => r.GetAllPagedAsync(request.Page, request.Size), Times.Once);
        _mockMapper.Verify(m => m.Map<List<GetAuctionItemsResponse>>(dbItems), Times.Once);
    }
}