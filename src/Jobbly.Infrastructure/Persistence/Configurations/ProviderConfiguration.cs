using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("providers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.IntegrationType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.LastError)
            .HasMaxLength(2000);
    }
}
