using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;
using Gavel.Domain.Entities;
using Gavel.Domain.Exceptions;
using Gavel.Domain.ValueObjects;
using Gavel.Tests.Helpers;
using Gavel.Infrastructure;
using Moq;
using System.Runtime.Serialization;

namespace Gavel.Tests.Application.Handlers;

public class GetAuctionItemByIdHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetAuctionItemByIdHandler _handler;

    public GetAuctionItemByIdHandlerTests()
    {
        _context = (ApplicationDbContext)FormatterServices.GetUninitializedObject(typeof(ApplicationDbContext));
        _mockMapper = new Mock<IMapper>();

        _handler = new GetAuctionItemByIdHandler(_context, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WhenItemExists_ReturnsAuctionItemResponse()
    {
        // Arrange
        var auctionItem = new AuctionItem("Test Auction", "Test Description", new Money(100m), DateTime.UtcNow.AddDays(1));
        var bid = new Bid(new Money(150m), auctionItem.Id, Guid.NewGuid());
        auctionItem.PlaceBid(bid);
        _context.AuctionItems = new List<AuctionItem> { auctionItem }.ToMockDbSet().Object;

        _mockMapper
            .Setup(m => m.Map<GetAuctionItemByIdResponse>(It.IsAny<AuctionItem>()))
            .Returns((AuctionItem item) => new GetAuctionItemByIdResponse
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                InitialPrice = item.InitialPrice.Amount,
                CurrentPrice = item.CurrentPrice.Amount,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                Status = item.Status,
                RowVersion = item.RowVersion,
                Bids = []
            });

        var query = new GetAuctionItemByIdQuery { Id = auctionItem.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(auctionItem.Id, result.Id);
        Assert.Equal("Test Auction", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(100m, result.InitialPrice);
        Assert.Equal(150m, result.CurrentPrice);

        _mockMapper.Verify(m => m.Map<GetAuctionItemByIdResponse>(It.IsAny<AuctionItem>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _context.AuctionItems = new List<AuctionItem>().ToMockDbSet().Object;
        var query = new GetAuctionItemByIdQuery { Id = nonExistentId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None)
        );

        Assert.Contains(nonExistentId.ToString(), exception.Message);
        _mockMapper.Verify(m => m.Map<GetAuctionItemByIdResponse>(It.IsAny<AuctionItem>()), Times.Never);
    }
}

