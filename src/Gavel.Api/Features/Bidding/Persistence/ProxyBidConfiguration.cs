using Gavel.Core.Domain.Bidding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Api.Features.Bidding.Persistence;

public sealed class ProxyBidConfiguration : IEntityTypeConfiguration<ProxyBid>
{
    public void Configure(EntityTypeBuilder<ProxyBid> builder)
    {
        builder.ToTable("ProxyBids");
        
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.LotId, e.MaxAmount, e.CreatedAt })
            .HasDatabaseName("IX_ProxyBids_LotId_MaxAmount_CreatedAt")
            .IsDescending(false, true, false);

        builder.Property(e => e.MaxAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.Property(e => e.BidderId)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(e => e.LotId)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
