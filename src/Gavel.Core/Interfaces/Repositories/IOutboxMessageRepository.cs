using Gavel.Domain.Entities;

namespace Gavel.Domain.Interfaces.Repositories;

public interface IOutboxMessageRepository : IRepository<OutboxMessage> { }