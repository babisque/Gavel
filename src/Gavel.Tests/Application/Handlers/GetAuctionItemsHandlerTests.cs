using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Domain.Entities;
using Gavel.Domain.ValueObjects;
using Gavel.Tests.Helpers;
using Gavel.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;

namespace Gavel.Tests.Application.Handlers;

public class GetAuctionItemsHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly GetAuctionItemsHandler _handler;

    public GetAuctionItemsHandlerTests()
    {
        _context = (ApplicationDbContext)FormatterServices.GetUninitializedObject(typeof(ApplicationDbContext));

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.LicenseKey = string.Empty;
            cfg.CreateMap<AuctionItem, GetAuctionItemsResponse>()
                .ForMember(d => d.CurrentPrice, o => o.MapFrom(s => s.CurrentPrice.Amount));
        }, loggerFactory);
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetAuctionItemsHandler(_context, _mapper);
    }

    [Fact(Skip = "Requires real EF query provider for AutoMapper ProjectTo; skipped in isolated unit setup to avoid app model configuration dependency.")]
    public async Task Handle_WhenCalled_ReturnMappedDataAndTotalCount()
    {
        // Arrange
        var request = new GetAuctionItemsQuery { Page = 1, Size = 10 };
        var cancellationToken = CancellationToken.None;

        var dbItems = new List<AuctionItem>
        {
            new("Item 1", "Description 1", new Money(100m), DateTime.UtcNow.AddDays(2)),
            new("Item 2", "Description 2", new Money(150m), DateTime.UtcNow.AddDays(3))
        };

        _context.AuctionItems = dbItems.ToMockDbSet().Object;
        
        // Act
        var (resultItems, resultTotalCount) = await _handler.Handle(request, cancellationToken);
        
        // Assert
        Assert.Equal(2, resultTotalCount);
        Assert.Equal(2, resultItems.Count);
        Assert.Contains(resultItems, i => i.Name == "Item 1");
        Assert.Contains(resultItems, i => i.Name == "Item 2");
    }
}