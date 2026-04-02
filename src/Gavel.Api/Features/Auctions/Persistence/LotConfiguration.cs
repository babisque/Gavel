namespace Gavel.Api.Features.Auctions.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Gavel.Core.Domain.Lots;

public sealed class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.StartingPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CurrentPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.AdminFees)
            .HasPrecision(18, 2);

        builder.Property(e => e.State)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // EF Core 10 Complex Property for the mandatory Commission VO
        builder.ComplexProperty(e => e.Commission, cb =>
        {
            cb.Property(c => c.Rate).HasColumnName("CommissionRate").HasPrecision(5, 4);
        });

        // Native AOT compatible JSON column for Photos
        builder.OwnsMany(e => e.Photos, pb =>
        {
            pb.ToJson();
            pb.Property(p => p.Url).HasMaxLength(2048);
        });

        // Native AOT compatible JSON column for Public Notice History (Auditability)
        builder.OwnsMany(e => e.NoticeHistory, nb =>
        {
            nb.ToJson();
            nb.Property(n => n.Url).HasMaxLength(2048);
            nb.Property(n => n.Version).HasMaxLength(50);
        });
    }
}
