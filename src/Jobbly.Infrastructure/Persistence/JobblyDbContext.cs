using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Persistence;

public sealed class JobblyDbContext(DbContextOptions<JobblyDbContext> options)
    : DbContext(options), IJobblyDbContext
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<CanonicalJob> CanonicalJobs => Set<CanonicalJob>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobblyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
