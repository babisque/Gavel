using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    protected Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    /// <summary>
    /// find all table entities
    /// </summary>
    public async Task<(IReadOnlyCollection<TEntity> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();

        var items = await _dbSet
            .AsNoTracking()
            .OrderBy(x => x)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }
}