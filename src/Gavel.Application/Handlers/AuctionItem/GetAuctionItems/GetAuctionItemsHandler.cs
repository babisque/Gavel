using AutoMapper;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

public class GetAuctionItemsHandler(IAuctionItemRepository repository, IMapper mapper) : IRequestHandler<GetAuctionItemsQuery, (List<GetAuctionItemsResponse> Items, int TotalCount)>
{
    public async Task<(List<GetAuctionItemsResponse> Items, int TotalCount)> Handle(GetAuctionItemsQuery request,
        CancellationToken cancellationToken)
    {
        var (auctionItems, totalCount) = await repository.GetAllPagedAsync(request.Page, request.Size);
        var response = mapper.Map<List<GetAuctionItemsResponse>>(auctionItems);
        
        return (response, totalCount);
    }
}