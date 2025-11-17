using AutoMapper;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

public class GetAuctionItemsHandler(IAuctionItemRepository repository, IMapper mapper) : IRequestHandler<GetAuctionItemsQuery, List<GetAuctionItemsResponse>>
{
    public async Task<List<GetAuctionItemsResponse>> Handle(GetAuctionItemsQuery request,
        CancellationToken cancellationToken)
    {
        var auctionItems = await repository.GetAllPagedAsync(request.Page, request.Size);
        var response = mapper.Map<List<GetAuctionItemsResponse>>(auctionItems);
        
        return response;
    }
}