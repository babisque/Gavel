using Gavel.Core.Domain.Bidding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Api.Features.Bidding.Persistence;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids");
        
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.LotId)
            .HasDatabaseName("IX_Bids_LotId")
            .IsUnique(false);

        builder.HasIndex(e => new { e.LotId, e.Amount })
            .HasDatabaseName("IX_Bids_LotId_Amount_DESC")
            .IsDescending(false, true);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.Timestamp)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.SourceIP)
            .HasMaxLength(50)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.BidderId)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.LotId)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
