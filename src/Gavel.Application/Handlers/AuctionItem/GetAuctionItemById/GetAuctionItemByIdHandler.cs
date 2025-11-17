using AutoMapper;
using Gavel.Application.Exceptions;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;

public class GetAuctionItemByIdHandler(IAuctionItemRepository repository, IMapper mapper) : IRequestHandler<GetAuctionItemByIdQuery, GetAuctionItemByIdResponse>
{
    public async Task<GetAuctionItemByIdResponse> Handle(GetAuctionItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id);
        if (item is null)
            throw new NotFoundException($"AuctionItem with ID {request.Id} not found.");
        
        return mapper.Map<GetAuctionItemByIdResponse>(item);
    }
}