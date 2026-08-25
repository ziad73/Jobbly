using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Common;

// This interface is how the Application layer talks to the database without
// depending on the Infrastructure project. It exposes the DbSet directly, so
// services get the full power of EF Core and LINQ - no repository in between.
public interface IJobblyDbContext
{
    DbSet<Provider> Providers { get; }
    DbSet<Company> Companies { get; }
    DbSet<Job> Jobs { get; }
    DbSet<CanonicalJob> CanonicalJobs { get; }
    DbSet<PipelineRun> PipelineRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
