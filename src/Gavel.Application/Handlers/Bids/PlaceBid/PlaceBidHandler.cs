using AutoMapper;
using Gavel.Application.Exceptions;
using Gavel.Application.Handlers.Bid.PlaceBid;
using Gavel.Application.Interfaces;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidHandler(
    IBidRepository bidRepository, 
    IAuctionItemRepository auctionItemRepository, 
    IBidNotificationService bidNotificationService, 
    IMapper mapper)
    : IRequestHandler<PlaceBidCommand>
{

    public async Task Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = await auctionItemRepository.GetByIdAsync(request.AuctionItemId);
        if (auctionItem is null)
            throw new NotFoundException($"Auction item with ID {request.AuctionItemId} not found.");
        
        if (auctionItem.Status is not AuctionStatus.Active)
            throw new ApplicationException($"Auction item with ID {request.AuctionItemId} is not active.");
        
        if (request.Amount <= auctionItem.CurrentPrice)
            throw new ApplicationException($"Bid amount must be greater than the current price of {auctionItem.CurrentPrice}.");
        
        var bid = mapper.Map<Domain.Entities.Bid>(request);
        bid.TimeStamp = DateTime.UtcNow;

        var createdBid = await bidRepository.CreateAsync(bid);
        auctionItem.CurrentPrice = request.Amount;

        await auctionItemRepository.UpdateAsync(auctionItem);
        
        await bidNotificationService.NotifyNewBidAsync(createdBid);

    }
}