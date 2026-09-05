using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.ToTable("user_skills");

        builder.HasKey(s => s.Id);

        // One row per skill per user
        builder.HasIndex(s => new { s.UserId, s.Name })
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);
    }
}