using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Application.Profiles;
using Gavel.Domain.Entities;
using Gavel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gavel.Tests.Application.Handlers;

public class GetAuctionItemsHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly GetAuctionItemsHandler _handler;

    public GetAuctionItemsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        _context = new ApplicationDbContext(options);

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.LicenseKey = string.Empty;
            cfg.AddProfile<AuctionItemsMapper>();
            cfg.AddProfile<BidMapper>();
        }, loggerFactory);
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetAuctionItemsHandler(_context, _mapper);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnMappedDataAndTotalCount()
    {
        // Arrange
        var request = new GetAuctionItemsQuery { Page = 1, Size = 10 };
        var cancellationToken = CancellationToken.None;

        var dbItems = new List<AuctionItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Item 1", StartTime = DateTime.UtcNow.AddDays(1), RowVersion = new byte[8] },
            new() { Id = Guid.NewGuid(), Name = "Item 2", StartTime = DateTime.UtcNow.AddDays(2), RowVersion = new byte[8] }
        };
        
        await _context.AuctionItems.AddRangeAsync(dbItems, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Act
        var (resultItems, resultTotalCount) = await _handler.Handle(request, cancellationToken);
        
        // Assert
        Assert.Equal(2, resultTotalCount);
        Assert.Equal(2, resultItems.Count);
        Assert.Equal("Item 1", resultItems[0].Name);
        Assert.Equal("Item 2", resultItems[1].Name);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}