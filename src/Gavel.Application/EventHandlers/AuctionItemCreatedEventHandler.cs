using System.Text.Json;
using Gavel.Domain.Constants;
using Gavel.Domain.Entities;
using Gavel.Domain.Events;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.EventHandlers;

public class AuctionItemCreatedEventHandler(IOutboxMessageRepository outboxMessageRepository)
    : INotificationHandler<AuctionItemCreatedEvent>
{
    public async Task Handle(AuctionItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        var jobIntent = new
        {
            notification.AuctionItemId, notification.EndTime
        };

        var outboxMessage = new OutboxMessage
        {
            Type = OutboxMessageTypes.ScheduleAuctionClose,
            Payload = JsonSerializer.Serialize(jobIntent)
        };

        await outboxMessageRepository.AddAsync(outboxMessage, cancellationToken);
    }
}