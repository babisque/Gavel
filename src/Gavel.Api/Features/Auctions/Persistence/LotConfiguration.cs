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

        builder.Property(e => e.MinimumIncrement)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CurrentPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.Property(e => e.AdminFees)
            .HasPrecision(18, 2);

        builder.Property(e => e.State)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(e => e.Commission, cb =>
        {
            cb.Property(c => c.Rate).HasColumnName("CommissionRate").HasPrecision(5, 4);
        });

        builder.OwnsMany(e => e.Photos, pb =>
        {
            pb.HasKey(p => p.Id);
            pb.Property(p => p.Url).HasMaxLength(2048).IsRequired();
            pb.Property(p => p.Order).IsRequired();
            
            if (builder.Metadata.Model?.ToString().Contains("Npgsql") == true)
            {
                // pb.ToJson(); 
            }
        });

        builder.OwnsMany(e => e.NoticeHistory, nb =>
        {
            nb.HasKey(n => n.Id);
            nb.Property(n => n.Url).HasMaxLength(2048).IsRequired();
            nb.Property(n => n.Version).HasMaxLength(50).IsRequired();
            nb.Property(n => n.AttachedAt).IsRequired();

            if (builder.Metadata.Model?.ToString().Contains("Npgsql") == true)
            {
                // nb.ToJson();
            }
        });
    }
}
