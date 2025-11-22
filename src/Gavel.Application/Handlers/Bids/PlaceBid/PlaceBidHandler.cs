using AutoMapper;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Exceptions;
using Gavel.Domain.Interfaces.Services;
using Gavel.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidHandler(
    ApplicationDbContext context,
    IBidNotificationService bidNotificationService, 
    IMapper mapper)
    : IRequestHandler<PlaceBidCommand>
{
    public async Task Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = await context.AuctionItems.FindAsync([request.AuctionItemId], cancellationToken);
        
        if (auctionItem is null)
            throw new NotFoundException($"Auction item {request.AuctionItemId} not found");
        
        if (auctionItem.Status != AuctionStatus.Active)
            throw new ConflictException("Auction is not active.");
    
        if (auctionItem.EndTime < DateTime.UtcNow)
            throw new ConflictException("Auction has ended.");
        
        if (request.Amount <= auctionItem.CurrentPrice)
            throw new ConflictException("Bid must be higher than current price.");
        
        var bid = mapper.Map<Bid>(request);
        bid.TimeStamp = DateTime.UtcNow;
        auctionItem.CurrentPrice = request.Amount;
        
        await context.Bids.AddAsync(bid, cancellationToken);
        
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The price has changed since you loaded the page. Please refresh and try again.");
        }
            
        await bidNotificationService.NotifyNewBidAsync(bid);
        
        // TODO: Implement RabbitMQ to process bids
        // The worker processes bids sequentially, ensuring no crashes,
        // and notifies users via SignalR if they were outbid immediately,
        // rather than throwing an API exception.
    }
}