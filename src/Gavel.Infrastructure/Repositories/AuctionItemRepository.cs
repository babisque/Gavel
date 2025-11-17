using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Infrastructure.Repositories;

public class AuctionItemRepository(ApplicationDbContext dbContext)
    : Repository<AuctionItem>(dbContext), IAuctionItemRepository
{
}