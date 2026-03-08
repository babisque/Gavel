using Gavel.Domain.Entities;

namespace Gavel.Domain.Interfaces.Repositories;

public interface IAuctionItemRepository
{
    Task<AuctionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(AuctionItem item, CancellationToken cancellationToken);
    Task<(List<AuctionItem> Items, int TotalCount)> GetActiveItemsPaginatedAsync(int page, int size, CancellationToken cancellationToken);
}
