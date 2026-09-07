using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.ToTable("saved_jobs");

        builder.HasKey(s => s.Id);

        // One tracking row per job per user - a duplicate save is a 409.
        builder.HasIndex(s => new { s.UserId, s.CanonicalJobId })
            .IsUnique();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);
    }
}