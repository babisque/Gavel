using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

public class GetAuctionItemsHandler(ApplicationDbContext context, IMapper mapper) 
    : IRequestHandler<GetAuctionItemsQuery, (List<GetAuctionItemsResponse> Items, int TotalCount)>
{
    public async Task<(List<GetAuctionItemsResponse> Items, int TotalCount)> Handle(GetAuctionItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.AuctionItems.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        
        var auctionItems = await query
            .OrderBy(ai => ai.StartTime)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ProjectTo<GetAuctionItemsResponse>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return (auctionItems, totalCount);
    }
}