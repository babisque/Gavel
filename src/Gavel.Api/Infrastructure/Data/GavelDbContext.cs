namespace Gavel.Api.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Gavel.Core.Domain.Registration;
using Gavel.Core.Domain.Auctions;

public class GavelDbContext(DbContextOptions<GavelDbContext> options) : DbContext(options)
{
    public DbSet<Bidder> Bidders => Set<Bidder>();
    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GavelDbContext).Assembly);
    }
}
