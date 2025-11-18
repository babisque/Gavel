namespace Gavel.Domain.Interfaces.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// returns all entities asynchronously
    /// </summary>
    /// <returns>A readonly collection of all entities</returns>
    Task<(IReadOnlyCollection<TEntity> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize);
    
    /// <summary>
    /// returns an entity by its unique identifier asynchronously
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <returns>The entity if found; otherwise, null</returns>
    Task<TEntity?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// creates a new entity asynchronously
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <returns>The created entity</returns>
    Task<TEntity> CreateAsync(TEntity entity);
}
