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
        builder.ComplexProperty(ai => ai.InitialPrice, priceBuilder =>
        {
            priceBuilder.Property(p => p.Amount)
                .HasColumnName("InitialPrice")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            priceBuilder.Property(p => p.Currency)
                .HasColumnName("InitialPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
        
        builder.ComplexProperty(ai => ai.CurrentPrice, priceBuilder =>
        {
            priceBuilder.Property(p => p.Amount)
                .HasColumnName("CurrentPrice")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            priceBuilder.Property(p => p.Currency)
                .HasColumnName("CurrentPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

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
