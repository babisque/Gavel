using AutoMapper;
using Gavel.Application.Exceptions;
using Gavel.Application.Handlers.Bid.PlaceBid;
using Gavel.Application.Interfaces;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        
        var bid = mapper.Map<Domain.Entities.Bid>(request);
        bid.TimeStamp = DateTime.UtcNow;
        var createdBid = await unitOfWork.Bids.CreateAsync(bid);

        auctionItem.CurrentPrice = request.Amount;
        await unitOfWork.AuctionItems.UpdateAsync(auctionItem);

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The price has changed since you loaded the page. Please refresh and try again.");
        }
            
        await bidNotificationService.NotifyNewBidAsync(createdBid);
    }
}