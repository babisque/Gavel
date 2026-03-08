using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Domain.ValueObjects;
using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;

public class CreateAuctionItemHandler(
    IAuctionItemRepository auctionItemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAuctionItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateAuctionItemCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = new Domain.Entities.AuctionItem(
            request.Name,
            request.Description,
            new Money(request.InitialPrice),
            request.EndTime);

        await auctionItemRepository.AddAsync(auctionItem, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return auctionItem.Id;
    }
}