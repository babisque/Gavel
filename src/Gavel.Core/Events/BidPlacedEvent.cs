using Gavel.Domain.Entities;
using MediatR;

namespace Gavel.Domain.Events;

public record BidPlacedEvent(Bid Bid) : INotification;