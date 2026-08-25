using Microsoft.EntityFrameworkCore;
using Jobbly.Application.Common;

namespace Jobbly.Infrastructure.Persistence;

public sealed class JobblyDbContext(DbContextOptions<JobblyDbContext> options)
    : DbContext(options), IJobblyDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobblyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
