using System.Text.Json;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Jobbly.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);

        // Relationships
        builder.HasOne(j => j.CanonicalJob)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CanonicalJobId);

        builder.HasOne(j => j.Provider)
            .WithMany()
            .HasForeignKey(j => j.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Company)
            .WithMany()
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // One job per external id per source - natural dedup key
        builder.HasIndex(j => new { j.ProviderId, j.ExternalId })
            .IsUnique();

        builder.HasIndex(j => j.CanonicalJobId);
        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => j.PipelineStatus);
        builder.HasIndex(j => j.PostedAt);

        builder.HasIndex(j => j.DedupFingerprint);

        // Scalar columns
        builder.Property(j => j.DedupFingerprint)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(j => j.ExternalId)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.CompanyName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.Location)
            .HasMaxLength(300);

        builder.Property(j => j.RemoteType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.SeniorityLevel)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(j => j.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(j => j.SalaryCurrency)
            .HasMaxLength(10);

        builder.Property(j => j.SalaryPeriod)
            .HasMaxLength(20);

        builder.Property(j => j.DescriptionRaw)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(j => j.SourceUrl)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(j => j.PipelineStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        // jsonb primitive collections - queryable server-side (Contains, etc.)
        builder.Property(j => j.TechStack)
            .HasColumnType("jsonb");

        builder.Property(j => j.Requirements)
            .HasColumnType("jsonb");

        builder.Property(j => j.NiceToHaves)
            .HasColumnType("jsonb");

        // Free-form structured sections - opaque to queries, so a JSON round-trip conversion
        builder.Property(j => j.DescriptionStructured)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions.Default) as IReadOnlyDictionary<string, string>,
                new ValueComparer<IReadOnlyDictionary<string, string>>(
                    (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.OrderBy(kv => kv.Key).SequenceEqual(b.OrderBy(kv => kv.Key))),
                    a => a == null ? 0 : a.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value)),
                    a => a == null ? new Dictionary<string, string>() : new Dictionary<string, string>(a)));

        // Full-text search groundwork: generated tsvector -text search vector- over searchable text + GIN index.
        var searchVector = builder.Property<NpgsqlTsVector>("SearchVector");// {t}ext {s}earch vector column
        searchVector.IsGeneratedTsVectorColumn(
            "english",
            nameof(Job.Title),
            nameof(Job.CompanyName),
            nameof(Job.DescriptionRaw));

        builder.HasIndex("SearchVector")
            .HasMethod("gin");// Gloabal Inverted Index
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
    }
}
