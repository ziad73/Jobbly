using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class PipelineRunConfiguration : IEntityTypeConfiguration<PipelineRun>
{
    public void Configure(EntityTypeBuilder<PipelineRun> builder)
    {
        builder.ToTable("pipeline_runs");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Provider)
            .WithMany()
            .HasForeignKey(r => r.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Run history queries are always "latest runs for this provider"
        builder.HasIndex(r => new { r.ProviderId, r.StartedAt });

        builder.Property(r => r.ProviderSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(4000);
    }
}
