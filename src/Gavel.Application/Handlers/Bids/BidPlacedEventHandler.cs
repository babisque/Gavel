using Gavel.Domain.Events;
using Gavel.Domain.Interfaces.Services;
using MediatR;

namespace Gavel.Application.Handlers.Bids;

public class BidPlacedEventHandler(IBidNotificationService bidNotificationService) 
    : INotificationHandler<BidPlacedEvent>
{
    public async Task Handle(BidPlacedEvent notification, CancellationToken cancellationToken)
    {
        await bidNotificationService.NotifyNewBidAsync(notification.Bid);
    }
}