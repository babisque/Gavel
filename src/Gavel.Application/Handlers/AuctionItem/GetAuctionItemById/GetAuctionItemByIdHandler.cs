using AutoMapper;
using Gavel.Domain.Exceptions;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;

public class GetAuctionItemByIdHandler(ApplicationDbContext context, IMapper mapper) : IRequestHandler<GetAuctionItemByIdQuery, GetAuctionItemByIdResponse>
{
    public async Task<GetAuctionItemByIdResponse> Handle(GetAuctionItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await context.AuctionItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        
        if (item is null)
            throw new NotFoundException($"AuctionItem with ID {request.Id} not found.");
        
        return mapper.Map<GetAuctionItemByIdResponse>(item);
    }
}