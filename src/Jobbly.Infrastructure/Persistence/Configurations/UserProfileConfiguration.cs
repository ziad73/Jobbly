using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);

        // One profile per user
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Title)
            .HasMaxLength(100);

        builder.Property(p => p.SeniorityLevel)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.RemotePreference)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.SalaryCurrency)
            .HasMaxLength(10);

        builder.Property(p => p.SalaryPeriod)
            .HasMaxLength(20);

        // jsonb primitive collections - queryable server-side (Contains, etc.)
        builder.Property(p => p.TechStack)
            .HasColumnType("jsonb");

        builder.Property(p => p.Locations)
            .HasColumnType("jsonb");
    }
}