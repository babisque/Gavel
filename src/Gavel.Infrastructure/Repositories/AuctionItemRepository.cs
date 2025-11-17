using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class AuctionItemRepository(ApplicationDbContext dbContext)
    : Repository<AuctionItem>(dbContext), IAuctionItemRepository
{
    public override async Task<(IReadOnlyCollection<AuctionItem> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize)
    {
        var totalCount = await dbContext.AuctionItems.CountAsync();

        var items = await dbContext.AuctionItems
            .AsNoTracking()
            .OrderBy(ai => ai.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}