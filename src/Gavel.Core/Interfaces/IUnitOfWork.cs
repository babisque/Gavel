using Gavel.Domain.Interfaces.Repositories;

namespace Gavel.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAuctionItemRepository AuctionItems { get; }
    IBidRepository Bids { get; }
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}