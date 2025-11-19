using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Infrastructure.Repositories;

namespace Gavel.Infrastructure;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public IAuctionItemRepository AuctionItems { get; } = new AuctionItemRepository(context);
    public IBidRepository Bids { get; } = new BidRepository(context);
    public IOutboxMessageRepository OutboxMessages { get; } = new OutboxMessageRepository(context);
    
    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        context.Dispose();
    }
}