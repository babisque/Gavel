namespace Gavel.Domain.Interfaces.Repositories;

public interface IOutboxMessageRepository
{
    Task AddAsync(Entities.OutboxMessage message, CancellationToken cancellationToken);
}