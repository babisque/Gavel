using Microsoft.EntityFrameworkCore;
using Gavel.Core.Domain.Registration;
using Gavel.Core.Domain.Auctions;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Bidding;
using Gavel.Core.Domain.Settlements;

namespace Gavel.Api.Infrastructure.Data;

public class GavelDbContext(DbContextOptions<GavelDbContext> options) : DbContext(options)
{
    public DbSet<Bidder> Bidders => Set<Bidder>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<ProxyBid> ProxyBids => Set<ProxyBid>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GavelDbContext).Assembly);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            modelBuilder.Entity<Lot>().Ignore(l => l.RowVersion);
        }

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Type).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Content).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
