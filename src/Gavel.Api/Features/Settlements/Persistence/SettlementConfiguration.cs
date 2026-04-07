using Gavel.Core.Domain.Settlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gavel.Api.Features.Settlements.Persistence;

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LotId).IsRequired();
        builder.Property(e => e.BidderId).IsRequired();
        builder.Property(e => e.WinningBidId).IsRequired();
        builder.Property(e => e.IssuedAt).IsRequired();
        builder.Property(e => e.PaymentDeadline).IsRequired();
        builder.Property(e => e.PaidAt);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DigitalSignature)
            .HasMaxLength(2048);

        builder.Property(e => e.SaleNoteUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(1000);

        builder.ComplexProperty(e => e.PriceBreakdown, pb =>
        {
            pb.Property(p => p.BidAmount).HasColumnName("HammerPrice").HasPrecision(18, 2);
            pb.Property(p => p.CommissionAmount).HasColumnName("Commission").HasPrecision(18, 2);
            pb.Property(p => p.AdminFees).HasColumnName("AdminFees").HasPrecision(18, 2);
            pb.Property(p => p.Total).HasColumnName("TotalAmount").HasPrecision(18, 2);
        });

        builder.Property(e => e.LotId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        builder.Property(e => e.BidderId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        builder.Property(e => e.WinningBidId).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        builder.Property(e => e.IssuedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
    }
}
