using Gavel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Infrastructure.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        builder.Property(b => b.TimeStamp)
            .IsRequired();
        builder.HasOne(b => b.AuctionItem)
            .WithMany(ai => ai.Bids)
            .HasForeignKey(b => b.AuctionItemId)
            .IsRequired();
        builder.HasOne(b => b.Bidder)
            .WithMany(au => au.Bids)
            .HasForeignKey(b => b.BidderId)
            .IsRequired();
        builder.HasIndex(b => new { b.AuctionItemId, b.Amount });
    }
}
