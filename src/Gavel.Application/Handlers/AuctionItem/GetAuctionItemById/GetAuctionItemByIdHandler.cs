using AutoMapper;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;

public class GetAuctionItemByIdHandler(IAuctionItemRepository repository, IMapper mapper) : IRequestHandler<GetAuctionItemByIdQuery, GetAuctionItemByIdResponse>
{
    public async Task<GetAuctionItemByIdResponse> Handle(GetAuctionItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id);
        return mapper.Map<GetAuctionItemByIdResponse>(item);
    }
}