namespace Gavel.Api.Features.Registration.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Gavel.Core.Domain.Registration;

public sealed class BidderConfiguration : IEntityTypeConfiguration<Bidder>
{
    public void Configure(EntityTypeBuilder<Bidder> builder)
    {
        // State Machine & Audit Configuration
        builder.Property(e => e.State)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(150);

        builder.Property(e => e.TaxId)
            .HasMaxLength(14);

        builder.Property(e => e.Email)
            .HasMaxLength(254);

        builder.Property(e => e.StatusReason)
            .HasMaxLength(500);

        builder.Property(e => e.TermsVersion)
            .HasMaxLength(50);

        // Optimistic Concurrency
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Native AOT compatible JSON column for Documents (Value Objects)
        // Optimized for read performance as per Decree No. 21,981/1932 documentation requirements
        builder.OwnsMany(e => e.Documents, docBuilder =>
        {
            docBuilder.ToJson();
            docBuilder.Property(d => d.Url).HasMaxLength(2048);
        });
    }
}
