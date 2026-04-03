namespace Gavel.Api.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Gavel.Core.Domain.Registration;
using Gavel.Core.Domain.Auctions;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Bidding;

public class GavelDbContext(DbContextOptions<GavelDbContext> options) : DbContext(options)
{
    public DbSet<Bidder> Bidders => Set<Bidder>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<ProxyBid> ProxyBids => Set<ProxyBid>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GavelDbContext).Assembly);
    }
}
