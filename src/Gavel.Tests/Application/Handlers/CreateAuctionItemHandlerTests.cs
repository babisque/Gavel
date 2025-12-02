using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;
using Gavel.Application.Profiles;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace Gavel.Tests.Application.Handlers;

public class CreateAuctionItemHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISchedulerFactory> _mockSchedulerFactory;
    private readonly IMapper _mapper;
    private readonly CreateAuctionItemHandler _handler;

    public CreateAuctionItemHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockPublisher = new Mock<IPublisher>();
        var mockLogger = new Mock<ILogger<ApplicationDbContext>>();
        _context = new ApplicationDbContext(options, mockPublisher.Object, mockLogger.Object);

        _mockSchedulerFactory = new Mock<ISchedulerFactory>();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.LicenseKey = string.Empty;
            cfg.AddProfile<AuctionItemsMapper>();
            cfg.AddProfile<BidMapper>();
        }, loggerFactory);
        _mapper = mapperConfig.CreateMapper();

        _handler = new CreateAuctionItemHandler(_context, _mockSchedulerFactory.Object, _mapper);
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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        var auctionItem = await _context.AuctionItems.FindAsync(result);
        Assert.NotNull(auctionItem);
        Assert.Equal(command.Name, auctionItem.Name);
        Assert.Equal(command.Description, auctionItem.Description);
        Assert.Equal(command.InitialPrice, auctionItem.InitialPrice);
        Assert.Equal(command.InitialPrice, auctionItem.CurrentPrice);
        Assert.Equal(AuctionStatus.Active, auctionItem.Status);
        Assert.True(auctionItem.StartTime <= DateTime.UtcNow);

        var outboxMessage = await _context.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(outboxMessage);
        Assert.Contains(result.ToString(), outboxMessage.Payload);
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

        var beforeTime = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var afterTime = DateTime.UtcNow;

        // Assert
        var auctionItem = await _context.AuctionItems.FindAsync(result);
        Assert.NotNull(auctionItem);
        Assert.True(auctionItem.StartTime >= beforeTime);
        Assert.True(auctionItem.StartTime <= afterTime);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

