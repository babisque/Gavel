using Gavel.Domain.Entities;

namespace Gavel.Application.Interfaces;

public interface IBidNotificationService
{
    Task NotifyNewBidAsync(Bid newBid);
}