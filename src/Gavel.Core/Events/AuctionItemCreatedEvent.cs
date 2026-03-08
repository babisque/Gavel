using MediatR;

namespace Gavel.Domain.Events;

public record AuctionItemCreatedEvent(Guid AuctionItemId, DateTime EndTime) : INotification;