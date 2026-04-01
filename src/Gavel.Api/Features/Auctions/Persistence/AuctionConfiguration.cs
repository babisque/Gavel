namespace Gavel.Api.Features.Auctions.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Gavel.Core.Domain.Auctions;

public sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        // Participation & Qualification Rules
        builder.HasMany(e => e.RegisteredBidders)
            .WithMany();
    }
}
