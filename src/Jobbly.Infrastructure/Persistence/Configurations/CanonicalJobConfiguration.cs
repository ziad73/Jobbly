using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class CanonicalJobConfiguration : IEntityTypeConfiguration<CanonicalJob>
{
    public void Configure(EntityTypeBuilder<CanonicalJob> builder)
    {
        builder.ToTable("canonical_jobs");

        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.PrimaryJob)
            .WithMany()
            .HasForeignKey(c => c.PrimaryJobId)
            .OnDelete(DeleteBehavior.Restrict);

        // Main feed excludes archived jobs
        builder.HasIndex(c => c.IsArchived);
        builder.HasIndex(c => c.LastSeenAt);

        builder.Property(c => c.DedupConfidence)
            .HasColumnType("real");
    }
}
