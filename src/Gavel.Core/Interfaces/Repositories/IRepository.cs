namespace Gavel.Domain.Interfaces.Repositories;

public interface IRepository<TEntity> : IDisposable where TEntity : class
{
    /// <summary>
    /// returns all entities asynchronously
    /// </summary>
    /// <returns>A readonly collection of all entities</returns>
    Task<IReadOnlyCollection<TEntity>> GetAllPagedAsync(int page, int pageSize);
}
