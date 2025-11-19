using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class OutboxMessageRepository(DbContext context) : Repository<OutboxMessage>(context), IOutboxMessageRepository
{
    public override Task<(IReadOnlyCollection<OutboxMessage> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize)
    {
        throw new NotImplementedException();
    }
}