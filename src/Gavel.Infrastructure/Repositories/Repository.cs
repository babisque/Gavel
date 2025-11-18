using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    protected Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }
    
    public abstract Task<(IReadOnlyCollection<TEntity> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize);

    /// <summary>
    /// find entity by id
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// create new entity
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Update existing entity
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}