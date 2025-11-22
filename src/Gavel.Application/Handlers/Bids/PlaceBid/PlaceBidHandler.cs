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
        
        var bid = mapper.Map<Bid>(request);
        auctionItem.PlaceBid(bid);
        await context.SaveChangesAsync(cancellationToken);
        
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