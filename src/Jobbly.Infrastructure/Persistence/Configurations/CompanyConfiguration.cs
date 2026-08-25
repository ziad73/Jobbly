using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.Industry)
            .HasMaxLength(150);

        builder.Property(c => c.SizeRange)
            .HasMaxLength(50);

        builder.Property(c => c.HqLocation)
            .HasMaxLength(300);

        builder.Property(c => c.Description)
            .HasColumnType("text");
    }
}
