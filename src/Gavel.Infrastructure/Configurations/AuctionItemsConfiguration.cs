using Gavel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Infrastructure.Configurations;

public class AuctionItemsConfiguration : IEntityTypeConfiguration<AuctionItem>
{
    public void Configure(EntityTypeBuilder<AuctionItem> builder)
    {
        builder.ToTable("AuctionItems");
        builder.HasKey(ai => ai.Id);
        builder.Property(ai => ai.Name)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(ai => ai.InitialPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        builder.Property(ai => ai.CurrentPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        builder.Property(ai => ai.StartTime)
            .IsRequired();
        builder.Property(ai => ai.EndTime)
            .IsRequired();
        builder.Property(ai => ai.Status)
            .IsRequired();
        builder.HasMany(ai => ai.Bids)
            .WithOne(b => b.AuctionItem)
            .HasForeignKey(b => b.AuctionItemId);
        builder.Property(ai => ai.RowVersion)
            .IsRowVersion();
    }
}
