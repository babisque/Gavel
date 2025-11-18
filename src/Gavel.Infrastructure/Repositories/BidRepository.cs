using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class BidRepository(DbContext context) : Repository<Bid>(context), IBidRepository
{
    public override async Task<(IReadOnlyCollection<Bid> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize)
    {
        var totalCount = await context.Set<Bid>().CountAsync();

        var items = await context.Set<Bid>()
            .AsNoTracking()
            .OrderBy(b => b.Amount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}