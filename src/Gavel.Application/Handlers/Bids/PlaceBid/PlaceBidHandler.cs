using Gavel.Domain.Entities;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Domain.ValueObjects;
using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidHandler(
    IAuctionItemRepository auctionItemRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceBidCommand>
{
    public async Task Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = await auctionItemRepository.GetByIdAsync(request.AuctionItemId, cancellationToken);

        if (auctionItem is null)
            throw new NotFoundException($"Auction item with ID {request.AuctionItemId} not found.");

        var bid = new Bid(new Money(request.Amount), request.AuctionItemId, request.BidderId);
        auctionItem.PlaceBid(bid);

        await unitOfWork.SaveChangesAsync();

        // TODO: Implement RabbitMQ to process bids
        // The worker processes bids sequentially, ensuring no crashes,
        // and notifies users via SignalR if they were outbid immediately,
        // rather than throwing an API exception.
    }
}