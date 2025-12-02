using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;
using Gavel.Application.Profiles;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Exceptions;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class GetAuctionItemByIdHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly GetAuctionItemByIdHandler _handler;

    public GetAuctionItemByIdHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockPublisher = new Mock<IPublisher>();
        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();
        _context = new ApplicationDbContext(options, mockPublisher.Object, mockLogger.Object);

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.LicenseKey = string.Empty;
            cfg.AddProfile<AuctionItemsMapper>();
            cfg.AddProfile<BidMapper>();
        }, loggerFactory);
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetAuctionItemByIdHandler(_context, _mapper);
    }

    [Fact]
    public async Task Handle_WhenItemExists_ReturnsAuctionItemResponse()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var auctionItem = new AuctionItem
        {
            Id = itemId,
            Name = "Test Auction",
            Description = "Test Description",
            InitialPrice = 100m,
            CurrentPrice = 150m,
            Status = AuctionStatus.Active,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            RowVersion = new byte[8]
        };

        _context.AuctionItems.Add(auctionItem);
        await _context.SaveChangesAsync();

        var query = new GetAuctionItemByIdQuery { Id = itemId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.Id);
        Assert.Equal("Test Auction", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(100m, result.InitialPrice);
        Assert.Equal(150m, result.CurrentPrice);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = new GetAuctionItemByIdQuery { Id = nonExistentId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None)
        );

        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

