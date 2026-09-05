using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Persistence;

public sealed class JobblyDbContext(DbContextOptions<JobblyDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IJobblyDbContext
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<CanonicalJob> CanonicalJobs => Set<CanonicalJob>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobblyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}