using AutoMapper;
using Gavel.Application.Exceptions;
using Gavel.Application.Handlers.Bid.PlaceBid;
using Gavel.Application.Interfaces;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using MediatR;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidHandler(
    IUnitOfWork unitOfWork,
    IBidNotificationService bidNotificationService, 
    IMapper mapper)
    : IRequestHandler<PlaceBidCommand>
{

    public async Task Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = await unitOfWork.AuctionItems.GetByIdAsync(request.AuctionItemId);
        if (auctionItem is null)
            throw new NotFoundException($"Auction item with ID {request.AuctionItemId} not found.");
        
        if (auctionItem.Status is not AuctionStatus.Active)
            throw new ApplicationException($"Auction item with ID {request.AuctionItemId} is not active.");
        
        if (request.Amount <= auctionItem.CurrentPrice)
            throw new ApplicationException($"Bid amount must be greater than the current price of {auctionItem.CurrentPrice}.");
        
        var bid = mapper.Map<Domain.Entities.Bid>(request);
        bid.TimeStamp = DateTime.UtcNow;
        var createdBid = await unitOfWork.Bids.CreateAsync(bid);

        auctionItem.CurrentPrice = request.Amount;
        await unitOfWork.AuctionItems.UpdateAsync(auctionItem);

        await unitOfWork.CompleteAsync(cancellationToken);
            
        await bidNotificationService.NotifyNewBidAsync(createdBid);
    }
}