using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class AuctionItemRepository(ApplicationDbContext context) : IAuctionItemRepository
{
    public async Task<AuctionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AuctionItems
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(AuctionItem item, CancellationToken cancellationToken)
    {
        await context.AuctionItems.AddAsync(item, cancellationToken);
    }

    public async Task<(List<AuctionItem> Items, int TotalCount)> GetActiveItemsPaginatedAsync(int page, int size, CancellationToken cancellationToken)
    {
        var query = context.AuctionItems.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Where(ai => ai.Status == AuctionStatus.Active)
            .OrderBy(ai => ai.StartTime)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
